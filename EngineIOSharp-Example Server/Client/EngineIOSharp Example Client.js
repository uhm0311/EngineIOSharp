var socket = require('engine.io-client')('ws://127.0.0.1:1009/engine.io/EIO=3&transport=websocket', { transports: [ 'websocket' ] });

socket.on('open', function () {
	console.log('Connected!');
	socket.send('hi');

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