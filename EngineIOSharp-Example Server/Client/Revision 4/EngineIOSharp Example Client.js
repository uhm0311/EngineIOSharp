const { Socket } = require('engine.io-client');
const socket = new Socket('ws://127.0.0.1:1009/engine.io/?EIO=4', {
	upgrade: false,
});

socket.on('open', function () {
	console.log('Connected!');
	socket.send(Buffer.from([0, 1, 2, 3, 4, 5, 6]));

	socket.on('message', function (message) {
		console.log('Server : ' + message);
	});
	
	socket.on('close', function () {
		console.log('Disconnected!');
		process.exit();
	});
});

const readline = require('readline').createInterface({
	input: process.stdin,
	output: process.stdout
});

function onMessage (message) {
	if (message != '/exit') {
		socket.send(Buffer.from([0, 1, 2, 3, 4, 5, 6]));
		
		socket.send('Client says, ');
		socket.send(message);
		
		socket.send('Client says again, ');
		socket.send('Hello world!');
		
		readline.question('', onMessage);
	} else {
		process.exit();
	}
}

console.log('input /exit to exit program.');
readline.question('', onMessage);