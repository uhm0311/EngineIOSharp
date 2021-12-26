const engine = require('engine.io');
const http = require('http').createServer();
const server = engine.attach(http, {
	allowUpgrades: true,
	allowEIO3: true
});

const port = 1009;

http.listen(port, function() {
	console.log('Listening on ' + port);
	
	server.on('connection', function (socket) {	
        console.log('Client connected!');
        socket.send(Buffer.from([0, 1, 2, 3, 4, 5, 6]));
		
		socket.on('message', function (message) { 
            console.log('Client : ' + message);

			socket.send(message);
			socket.send(message);
		});
		
		socket.on('close', function(e, d) { 
			console.log('Client disconnected!');
			console.log(e);
			console.log(d);
		});

		socket.on('flush', function (packet) {
			console.log('Client flushed!');
			console.log(packet);
		});

		socket.on('drain', function () {
			console.log('Client drained!');
		});

		socket.on('packetCreate', function (packet) {
			console.log('Client packetCreate!');
			console.log(packet);
		});
	});
});