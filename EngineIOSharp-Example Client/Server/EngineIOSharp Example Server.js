var engine = require('engine.io');
var http = require('http').createServer();
var server = engine.attach(http);

var port = 1009;

http.listen(port, function() {
	console.log('Listening on ' + port);
	
	server.on('connection', function (socket) {	
		console.log('Client connected!');
		socket.send(Buffer.from([0, 1, 2, 3, 4, 5]));
		
		socket.on('message', function (message) { 
			console.log('Client : ' + message);
		});
		
		socket.on('close', function() { 
			console.log('Client disconnected!');
		});
	});
	
	server.on('connection', function (socket) {	
		console.log('Client connected!!');
	});
});