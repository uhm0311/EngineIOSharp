var socket = require('engine.io-client')('ws://127.0.0.1:1009/engine.io/EIO=3&transport=websocket', { transports: [ 'websocket' ] });

socket.on('open', function () {
	console.log('Connected!');
	socket.send('hi');

	socket.on('message', function (message) {
		console.log('Server : ' + message);
	});
	
	socket.on('close', function () {
		console.log('Disconnected!');
	});
});

const readline = require('readline').createInterface({
	input: process.stdin,
	output: process.stdout
});

readline.question('', function (message) {
	socket.send(message);
});