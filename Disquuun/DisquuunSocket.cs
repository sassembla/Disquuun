using System;
using System.Net;
using System.Net.Sockets;

namespace DisquuunCore {
    public class DisquuunSocket {
		public readonly string socketId;
		
		private Action<DisquuunSocket, string> SocketOpened;
		private Action<DisquuunSocket, Exception> SocketClosed;
		
		
		private SocketToken socketToken;
		
		public SocketState State () {
			return socketToken.socketState;
		}
		
		public bool SetBusy () {
			if (socketToken.socketState == SocketState.OPENED) {
				socketToken.socketState = SocketState.BUSY;
				return true;
			}
			
			return false;
		}
		
		public enum SocketState {
			OPENING,
			OPENED,			
			BUSY,
			
			
			DISPOSABLE_READY,
			DISPOSABLE_OPENING,
			DISPOSABLE_BUSY,
			
			
			CLOSING,
			CLOSED
		}
		
		public class SocketToken {
			public SocketState socketState;
			
			public readonly Socket socket;
			
			public byte[] receiveBuffer;
			public int readableDataLength;
			
			public readonly SocketAsyncEventArgs connectArgs;
			public readonly SocketAsyncEventArgs sendArgs;
			public readonly SocketAsyncEventArgs receiveArgs;
			
			public DisqueCommand currentCommand;
			public byte[] currentSendingBytes;
			
			public Func<DisqueCommand, DisquuunCore.DisquuunResult[], bool> AsyncCallback;
			
			public SocketToken (Socket socket, long bufferSize, SocketAsyncEventArgs connectArgs, SocketAsyncEventArgs sendArgs, SocketAsyncEventArgs receiveArgs) {
				this.socket = socket;
				
				this.receiveBuffer = new byte[bufferSize];
				
				this.connectArgs = connectArgs;
				this.sendArgs = sendArgs;
				this.receiveArgs = receiveArgs;
				
				this.connectArgs.UserToken = this;
				this.sendArgs.UserToken = this;
				this.receiveArgs.UserToken = this;
				
				this.receiveArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
			}
		}
		
		public DisquuunSocket (IPEndPoint endPoint, long bufferSize) {
			this.socketId = Guid.NewGuid().ToString();
			
			var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			
			var connectArgs = new SocketAsyncEventArgs();
			connectArgs.AcceptSocket = clientSocket;
			connectArgs.RemoteEndPoint = endPoint;
			
			var sendArgs = new SocketAsyncEventArgs();
			sendArgs.AcceptSocket = clientSocket;
			sendArgs.RemoteEndPoint = endPoint;
			sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);
			
			var receiveArgs = new SocketAsyncEventArgs();
			receiveArgs.AcceptSocket = clientSocket;
			receiveArgs.RemoteEndPoint = endPoint;
			receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceived);
					
			socketToken = new SocketToken(clientSocket, bufferSize, connectArgs, sendArgs, receiveArgs);
			socketToken.socketState = SocketState.DISPOSABLE_READY;
			
			// not start connecting yet.
		}
		
		public DisquuunSocket (IPEndPoint endPoint, long bufferSize, Action<DisquuunSocket, string> SocketOpened, Action<DisquuunSocket, Exception> SocketClosed) {
			this.socketId = Guid.NewGuid().ToString();
			
			this.SocketOpened = SocketOpened;
			this.SocketClosed = SocketClosed;
			
			var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			
			var connectArgs = new SocketAsyncEventArgs();
			connectArgs.AcceptSocket = clientSocket;
			connectArgs.RemoteEndPoint = endPoint;
			connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);
			
			var sendArgs = new SocketAsyncEventArgs();
			sendArgs.AcceptSocket = clientSocket;
			sendArgs.RemoteEndPoint = endPoint;
			sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);
			
			var receiveArgs = new SocketAsyncEventArgs();
			receiveArgs.AcceptSocket = clientSocket;
			receiveArgs.RemoteEndPoint = endPoint;
			receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceived);
						
			socketToken = new SocketToken(clientSocket, bufferSize, connectArgs, sendArgs, receiveArgs); 
			socketToken.socketState = SocketState.OPENING;
			
			// start connect.
			StartConnectAsync(clientSocket, socketToken.connectArgs);
		}
		
		public void StartConnectAsync (Socket clientSocket, SocketAsyncEventArgs connectArgs) {
			if (!clientSocket.ConnectAsync(socketToken.connectArgs)) OnConnect(clientSocket, connectArgs);
		}
		
		/*
			Core methods of Disquuun.
		*/
		
		/**
			method for Sync execution of specific Disque command.
			DEPRECATED. only use for testing.
		*/
		public DisquuunResult[] DEPRECATED_Sync (DisqueCommand command, byte[] data) {
			socketToken.socket.Send(data);
			
			// TestLogger.Log("send失敗とかもありえるはず。");
			
			var currentLength = 0;
			var scanResult = new DisquuunAPI.ScanResult(false);
			
			while (true) {
				// waiting for head of transferring data or rest of data.
				socketToken.socket.Receive(socketToken.receiveBuffer, currentLength, 1, SocketFlags.None);
				currentLength = currentLength + 1;
				
				var available = socketToken.socket.Available;
				var readableLength = currentLength + available;
				{
					if (socketToken.receiveBuffer.Length < readableLength) {
						TestLogger.Log("サイズオーバーしてる " + socketToken.receiveBuffer.Length + " vs:" + readableLength);
						Array.Resize(ref socketToken.receiveBuffer, readableLength);
					} else {
						// TestLogger.Log("まだサイズオーバーしてない " + socketToken.receiveBuffer.Length + " vs:" + readableLength + " が、読み込みの過程でサイズオーバーしそう。");
					}
				}
				
				// read rest.
				socketToken.socket.Receive(socketToken.receiveBuffer, currentLength, available, SocketFlags.None);
				currentLength = currentLength + available;
				
				scanResult = DisquuunAPI.ScanBuffer(command, socketToken.receiveBuffer, currentLength);
				if (scanResult.isDone) break;
				
				// continue reading data from socket.
				// if need, prepare for next 1 byte.
				if (socketToken.receiveBuffer.Length == readableLength) {
					TestLogger.Log("サイズオーバーの拡張をしてて、さらにもう1byte以上読む必要がある。");
					Array.Resize(ref socketToken.receiveBuffer, socketToken.receiveBuffer.Length + 1);
				}
			}
			
			socketToken.socketState = SocketState.OPENED;
			return scanResult.data;
		}

		/**
			method for Async execution of specific Disque command.
		*/		
		public void Async (DisqueCommand command, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback) {
			switch (socketToken.socketState) {
				case SocketState.BUSY: {
					StartReceiveAndSendDataAsync(command, data, Callback);
					break;
				}
				case SocketState.DISPOSABLE_READY: {
					// ready disposable connect callback.
					Action<object, SocketAsyncEventArgs> OnConnectedDisposable = (object unused, SocketAsyncEventArgs args) => {
						socketToken.socketState = SocketState.DISPOSABLE_BUSY;
						StartReceiveAndSendDataAsync(command, data, Callback);
					};
					
					socketToken.connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectedDisposable);
					socketToken.socketState = SocketState.DISPOSABLE_OPENING;
					
					StartConnectAsync(socketToken.socket, socketToken.connectArgs);
					break;
				}
			}
		}
		
		/**
			method for start Looping of specific Disque command.
		*/
		public void Loop (DisqueCommand command, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback) {
			switch (socketToken.socketState) {
				case SocketState.BUSY: {
					StartReceiveAndSendDataAsync(command, data, Callback);
					break;
				}
				case SocketState.DISPOSABLE_READY: {
					// ready disposable connect callback.
					Action<object, SocketAsyncEventArgs> OnConnectedDisposable = (object unused, SocketAsyncEventArgs args) => {
						socketToken.socketState = SocketState.DISPOSABLE_BUSY;
						StartReceiveAndSendDataAsync(command, data, Callback);
					};
					
					socketToken.connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectedDisposable);
					socketToken.socketState = SocketState.DISPOSABLE_OPENING;
					
					StartConnectAsync(socketToken.socket, socketToken.connectArgs);
					break;
				}
			}
		}
		
		
		/*
			default pooled socket + disposable socket shared 
		*/
		private void StartReceiveAndSendDataAsync (DisqueCommand command, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback) {
			// ready for receive.
			socketToken.readableDataLength = 0;
			socketToken.receiveArgs.SetBuffer(socketToken.receiveBuffer, 0, socketToken.receiveBuffer.Length);
			if (!socketToken.socket.ReceiveAsync(socketToken.receiveArgs)) OnReceived(socketToken.socket, socketToken.receiveArgs);
			
			socketToken.currentCommand = command;
			socketToken.currentSendingBytes = data;
			socketToken.AsyncCallback = Callback;
			socketToken.sendArgs.SetBuffer(data, 0, data.Length);
			if (!socketToken.socket.SendAsync(socketToken.sendArgs)) OnSend(socketToken.socket, socketToken.sendArgs); 
		}
		
		private void StartCloseAsync () {
			// 
			var closeEventArgs = new SocketAsyncEventArgs();
			closeEventArgs.UserToken = socketToken;
			closeEventArgs.AcceptSocket = socketToken.socket;
			closeEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnClosed);
			
			if (!socketToken.socket.DisconnectAsync(closeEventArgs)) OnClosed(socketToken.socket, closeEventArgs);
		}
		
		/*
			handlers
		*/
		private void OnConnect (object unused, SocketAsyncEventArgs args) {
			var token = (SocketToken)args.UserToken;
			switch (token.socketState) {
				case SocketState.OPENING: {
					if (args.SocketError != SocketError.Success) {
						token.socketState = SocketState.CLOSED;
						var error = new Exception("connect error:" + args.SocketError.ToString());
						
						SocketClosed(this, error);
						return;
					}
					
					token.socketState = SocketState.OPENED;
					SocketOpened(this, socketId);
					return;
				}
				default: {
					throw new Exception("socket state does not correct:" + token.socketState);
				}
			}
		}
		
		private void OnClosed (object unused, SocketAsyncEventArgs args) {
			var token = (SocketToken)args.UserToken;
			switch (token.socketState) {
				case SocketState.CLOSED: {
					// do nothing.
					break;
				}
				default: {
					token.socketState = SocketState.CLOSED;
					break;
				}
			}
		}
		
		private void OnSend (object unused, SocketAsyncEventArgs args) {
			var socketError = args.SocketError;
			switch (socketError) {
				case SocketError.Success: {
					// do nothing.
					break;
				}
				default: {
					Disquuun.Log("まだエラーハンドルしてない。切断の一種なんだけど、非同期実行してるAPIに紐付けることができる。");
					// if (Error != null) {
					// 	var error = new Exception("send error:" + socketError.ToString());
					// 	Error(error);
					// }
					break;
				}
			}
		}
		
		private void OnReceived (object unused, SocketAsyncEventArgs args) {
			var token = (SocketToken)args.UserToken;
			
			if (args.SocketError != SocketError.Success) { 
				switch (token.socketState) {
					case SocketState.CLOSING:
					case SocketState.CLOSED: {
						// already closing, ignore.
						return;
					}
					default: {
						// show error, then close or continue receiving.
						Disquuun.Log("まだエラーハンドルしてない2。切断の一種なんだけど、非同期実行してるAPIに紐付けることができる、、、かなあ？　できない気もしてきたぞ。");
						// if (Error != null) {
						// 	var error = new Exception("receive error:" + args.SocketError.ToString() + " size:" + args.BytesTransferred);
						// 	Error(error);
						// }
						
						// connection is already closed.
						if (!IsSocketConnected(token.socket)) {
							Disconnect();
							return;
						}
						
						// continue receiving data. go to below.
						break;
					}
				}
			}
			
			if (0 < args.BytesTransferred) {
				var bytesAmount = args.BytesTransferred;
				
				// update as read completed.
				token.readableDataLength = token.readableDataLength + bytesAmount;
				
				var result = DisquuunAPI.ScanBuffer(token.currentCommand, token.receiveBuffer, token.readableDataLength);
				
				if (result.isDone) {
					var continuation = token.AsyncCallback(token.currentCommand, result.data);
					if (continuation) {
						// ready for loop receive.
						token.readableDataLength = 0;
						token.receiveArgs.SetBuffer(token.receiveBuffer, 0, token.receiveBuffer.Length);
						if (!token.socket.ReceiveAsync(token.receiveArgs)) OnReceived(token.socket, token.receiveArgs);
						
						token.sendArgs.SetBuffer(token.currentSendingBytes, 0, token.currentSendingBytes.Length);
						if (!token.socket.SendAsync(token.sendArgs)) OnSend(token.socket, token.sendArgs);
					} else {
						switch (token.socketState) {
							case SocketState.BUSY: {
								token.socketState = SocketState.OPENED;
								break;
							}
							case SocketState.DISPOSABLE_BUSY: {
								// close.
								StartCloseAsync();
								break;
							}
						}
					}
				} else {
					/*
						note that,
						
						SetBuffer([buffer], offset, count)'s "count" is, actually not count.
						 
						it's "offset" is "offset of receiving-data-window against buffer",
						but the "count" is actually "size limitation of next receivable data size".
						
						this "size" should be smaller than size of current bufferSize - offset && larger than 0.
						
						e.g.
							if buffer is buffer[10], offset can set 0 ~ 8, and,
							count should be 9 ~ 1.
						
						if vaiolate to this rule, ReceiveAsync never receive data. not good behaviour.
						
						and, the "buffer" is treated as pointer. this API treats the pointer of buffer directly.
						this means, when the byteTransferred is reaching to the size of "buffer", then you resize it to proper size,
						
						you should re-set the buffer's pointer by using SetBuffer API.
						
						
						actually, SetBuffer's parameters are below.
						
						socket.SetBuffer([bufferAsPointer], additionalDataOffset, receiveSizeLimit)
					*/
					
					var nextAdditionalBytesLength = token.socket.Available;
					
					if (token.readableDataLength == token.receiveBuffer.Length) {
						TestLogger.Log("次のデータが来るのが確定していて、かつバッファサイズが足りない。");
						Array.Resize(ref token.receiveBuffer, token.receiveArgs.Buffer.Length + nextAdditionalBytesLength);
					}
					
					var receivableCount = token.receiveBuffer.Length - token.readableDataLength;
					token.receiveArgs.SetBuffer(token.receiveBuffer, token.readableDataLength, receivableCount);
					if (!token.socket.ReceiveAsync(token.receiveArgs)) OnReceived(token.socket, token.receiveArgs);
				}	
			}
		}
		
		
		public void Disconnect (bool force=false) {
			if (force) {
				try {
					socketToken.socket.Close();
				} catch (Exception e) {
					Disquuun.Log("e:" + e);
				}
				return;
			}
			
			switch (socketToken.socketState) {
				case SocketState.CLOSING:
				case SocketState.CLOSED: {
					// do nothing
					break;
				}
				default: {
					socketToken.socketState = SocketState.CLOSING;
					
					StartCloseAsync();
					break;
				}
			}
		}
		
		
		/*
			utils
		*/
		
		private static bool IsSocketConnected (Socket s) {
			bool part1 = s.Poll(1000, SelectMode.SelectRead);
			bool part2 = (s.Available == 0);
			
			if (part1 && part2) return false;
			
			return true;
		}
	}
	
	
	public static class DisquuunExtension {
		public static DisquuunResult[] DEPRICATED_Sync (this DisquuunInput input) {	
			var socket = input.socket;
			return socket.DEPRECATED_Sync(input.command, input.data);
		}
		
		public static void Async (this DisquuunInput input, Action<DisqueCommand, DisquuunResult[]> Callback) {	
			var socket = input.socket;
			socket.Async(
				input.command, 
				input.data, 
				(command, resultBytes) => {
					Callback(command, resultBytes);
					return false;
				}
			);
		}
		
		public static void Loop (this DisquuunInput input, Func<DisqueCommand, DisquuunResult[], bool> Callback) {	
			var socket = input.socket;
			socket.Loop(input.command, input.data, Callback);
		}
	}
}