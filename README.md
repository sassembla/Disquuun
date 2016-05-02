#Disquuun

C# disque client.

lightweight, small, independent. not depends on any Redis code.  
"n" means "this is written in C#".

##usage
```C#
using DisquuunCore;

Disquuun disquuun;

string connectionId = "newDisqueConnectionId";

disquuun = new Disquuun(
	connectionId,
	"127.0.0.1", 
	7711,
	102400,// buffer size.
	connectedConId => {
		// connected.
	},
	(command, byteDatas) => {
		// receive result of commands.
		switch (command) {
			case Disquuun.DisqueCommand.ADDJOB: {
				var addedJobId = DisquuunDeserializer.AddJob(byteDatas);
				break;
			}
		}
	},
	(failedCommand, reason) => {
		// error on receiving datas.
	},
	e => {
		// error on client.
	},
	disconnectedConId => {
		// disconnected.
	}
);

disquuun.AddJob("testQ", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
```