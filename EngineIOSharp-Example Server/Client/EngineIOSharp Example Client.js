var socket = require('engine.io-client')('ws://127.0.0.1:1009/engine.io/?EIO=4');

socket.on('open', function () {
	console.log('Connected!');
	console.log(socket.id);

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
		socket.send(message);
		readline.question('', onMessage);
	} else {
		process.exit();
	}
}

console.log('input /exit to exit program.');
readline.question('', onMessage);