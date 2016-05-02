#Disquuun

C# disque client.

lightweight, small, independent. not depends on any Redis code.

##usage
```C#
using DisquuunCore;

private Disquuun disquuun;

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
```