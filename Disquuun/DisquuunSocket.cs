using System;
using System.Net;
using System.Net.Sockets;

namespace DisquuunCore
{
    public class DisquuunSocket {
		public readonly string socketId;
		
		private Action<DisquuunSocket, string> SocketOpened;
		private Action<DisquuunSocket, string, Exception> SocketClosed;
		
		
		private SocketToken socketToken;
		
		public SocketState State () {
			return socketToken.socketState;
		}
		
		public bool IsChoosable () {
			if (socketToken.socketState == SocketState.OPENED) return true;
			return false;
		}

		public void SetBusy () {
			socketToken.socketState = SocketState.BUSY;
		}
		
		public enum SocketState {
			OPENING,
			OPENED,			
			BUSY,

			SENDED,
			RECEIVED,
			
			DISPOSABLE_READY,
			DISPOSABLE_OPENING,
			DISPOSABLE_BUSY,
			DISPOSABLE_SENDED,
			DISPOSABLE_RECEIVED,
			
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

			public bool continuation;

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
		
		public DisquuunSocket (IPEndPoint endPoint, long bufferSize, Action<DisquuunSocket, string, Exception> SocketClosedAct) {
			this.socketId = Guid.NewGuid().ToString();
			
			this.SocketClosed = SocketClosedAct;

			try {
				var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				clientSocket.NoDelay = true;
				
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
			} catch (Exception e) {
				SocketClosed(this, "failed to create additioal socket.", e);
			}
		}
		
		public DisquuunSocket (IPEndPoint endPoint, long bufferSize, Action<DisquuunSocket, string> SocketOpenedAct, Action<DisquuunSocket, string, Exception> SocketClosedAct) {
			this.socketId = Guid.NewGuid().ToString();
			
			this.SocketOpened = SocketOpenedAct;
			this.SocketClosed = SocketClosedAct;
			try {
				var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				clientSocket.NoDelay = true;
				
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
			} catch (Exception e) {
				SocketClosed(this, "failed to create new socket.", e);
			}
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
						// Disquuun.Log("サイズオーバーしてる " + socketToken.receiveBuffer.Length + " vs:" + readableLength);
						Array.Resize(ref socketToken.receiveBuffer, readableLength);
					} else {
						// Disquuun.Log("まだサイズオーバーしてない " + socketToken.receiveBuffer.Length + " vs:" + readableLength + " が、読み込みの過程でサイズオーバーしそう。");
					}
				}
				
				// read rest.
				socketToken.socket.Receive(socketToken.receiveBuffer, currentLength, available, SocketFlags.None);
				currentLength = currentLength + available;
				
				scanResult = DisquuunAPI.ScanBuffer(command, socketToken.receiveBuffer, currentLength, socketId);
				if (scanResult.cursor == currentLength) break;
				
				// continue reading data from socket.
				// if need, prepare for next 1 byte.
				if (socketToken.receiveBuffer.Length == readableLength) {
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
					Action<object, SocketAsyncEventArgs> OnConnectedAdditional = (object unused, SocketAsyncEventArgs args) => {
						socketToken.socketState = SocketState.DISPOSABLE_BUSY;
						StartReceiveAndSendDataAsync(command, data, Callback);
					};
					
					socketToken.connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectedAdditional);
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
						
						SocketClosed(this, "connect failed.", error);
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
			switch (args.SocketError) {
				case SocketError.Success: {
					var token = args.UserToken as SocketToken;
					
					switch (token.socketState) {
						case SocketState.BUSY: {
							token.socketState = SocketState.SENDED;
							break;
						}
						case SocketState.RECEIVED: {
							if (token.continuation) {
								// ready for next loop receive.
								token.readableDataLength = 0;
								token.receiveArgs.SetBuffer(token.receiveBuffer, 0, token.receiveBuffer.Length);
								if (!token.socket.ReceiveAsync(token.receiveArgs)) OnReceived(token.socket, token.receiveArgs);

								token.sendArgs.SetBuffer(token.currentSendingBytes, 0, token.currentSendingBytes.Length);
								if (!token.socket.SendAsync(token.sendArgs)) OnSend(token.socket, token.sendArgs);
								return;
							}
							
							token.socketState = SocketState.OPENED;
							return;
						}


						case SocketState.DISPOSABLE_BUSY: {
							token.socketState = SocketState.DISPOSABLE_SENDED;
							break;
						}
						case SocketState.DISPOSABLE_RECEIVED: {
							if (token.continuation) {
								// token.socketState = SocketState.DISPOSABLE_BUSY;

								// ready for next loop receive.
								token.readableDataLength = 0;
								token.receiveArgs.SetBuffer(token.receiveBuffer, 0, token.receiveBuffer.Length);
								if (!token.socket.ReceiveAsync(token.receiveArgs)) OnReceived(token.socket, token.receiveArgs);

								token.sendArgs.SetBuffer(token.currentSendingBytes, 0, token.currentSendingBytes.Length);
								if (!token.socket.SendAsync(token.sendArgs)) OnSend(token.socket, token.sendArgs);
								return;
							}
							
							StartCloseAsync();
							break;
						}
					}
					return;
				}
				default: {
					Disquuun.Log("onsend error, " + args.SocketError);
					// if (Error != null) {
					// 	var error = new Exception("send error:" + socketError.ToString());
					// 	Error(error);
					// }
					return;
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
						Disquuun.Log("onReceive error:" + args.SocketError);
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
			
			if (args.BytesTransferred == 0) return;

			var bytesAmount = args.BytesTransferred;
			
			// update as read completed.
			token.readableDataLength = token.readableDataLength + bytesAmount;

			var result = DisquuunAPI.ScanBuffer(token.currentCommand, token.receiveBuffer, token.readableDataLength, socketId);

			// data is totally read and executable.
			if (result.cursor == token.readableDataLength) {
				var continuation = token.AsyncCallback(token.currentCommand, result.data);

				// update continuation status.
				token.continuation = continuation;

				if (continuation) {
					switch (token.socketState) {
						case SocketState.BUSY: {
							token.socketState = SocketState.RECEIVED;
							break;
						}
						case SocketState.SENDED: {
							// token.socketState = SocketState.BUSY;

							// ready for next loop receive.
							token.readableDataLength = 0;
							token.receiveArgs.SetBuffer(token.receiveBuffer, 0, token.receiveBuffer.Length);
							if (!token.socket.ReceiveAsync(token.receiveArgs)) OnReceived(token.socket, token.receiveArgs);
							
							token.sendArgs.SetBuffer(token.currentSendingBytes, 0, token.currentSendingBytes.Length);
							if (!token.socket.SendAsync(token.sendArgs)) OnSend(token.socket, token.sendArgs);
							break;
						}

						// disposable.
						case SocketState.DISPOSABLE_BUSY: {
							token.socketState = SocketState.DISPOSABLE_RECEIVED;
							break;
						}
						case SocketState.DISPOSABLE_SENDED: {
							// token.socketState = SocketState.DISPOSABLE_BUSY;

							// ready for next loop receive.
							token.readableDataLength = 0;
							token.receiveArgs.SetBuffer(token.receiveBuffer, 0, token.receiveBuffer.Length);
							if (!token.socket.ReceiveAsync(token.receiveArgs)) OnReceived(token.socket, token.receiveArgs);
							
							token.sendArgs.SetBuffer(token.currentSendingBytes, 0, token.currentSendingBytes.Length);
							if (!token.socket.SendAsync(token.sendArgs)) OnSend(token.socket, token.sendArgs);
							break;
						}
						default: {
							// closing or other state. should close.
							break;
						}
					}
					return;
				}

				// end of loop or end of async.

				switch (token.socketState) {
					case SocketState.BUSY: {
						token.socketState = SocketState.RECEIVED;
						break;
					}
					case SocketState.SENDED: {
						token.socketState = SocketState.OPENED;
						break;
					}

					// disposable.
					case SocketState.DISPOSABLE_BUSY: {
						token.socketState = SocketState.DISPOSABLE_RECEIVED;
						break;
					}
					case SocketState.DISPOSABLE_SENDED: {
						// disposable connection should be close after used.
						StartCloseAsync();
						break;
					}
					default: {
						break;
					}
				}
				return;
			}

			// not yet received all data.
			// continue receiving.

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
			
			if (token.readableDataLength == token.receiveBuffer.Length) Array.Resize(ref token.receiveBuffer, token.receiveArgs.Buffer.Length + nextAdditionalBytesLength);
			
			var receivableCount = token.receiveBuffer.Length - token.readableDataLength;
			token.receiveArgs.SetBuffer(token.receiveBuffer, token.readableDataLength, receivableCount);

			if (!token.socket.ReceiveAsync(token.receiveArgs)) OnReceived(token.socket, token.receiveArgs);
		}
		
		public void Disconnect (bool force=false) {
			if (force) {
				try {
					socketToken.socketState = SocketState.CLOSING;
					socketToken.socket.Close();
					socketToken.socketState = SocketState.CLOSED;
				} catch (Exception e) {
					Disquuun.Log("Disconnect e:" + e);
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
