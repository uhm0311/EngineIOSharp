var engine = require('engine.io', { 'transports': [ 'websocket' ] });
var http = require('http').createServer();
var server = engine.attach(http);

var port = 1009;

http.listen(port, function() {
	console.log('Listening on ' + port);
	
	server.on('connection', function (socket) {	
		console.log('Client connected!');
		
		socket.on('message', function (message) { 
            console.log('Client : ' + message);

            socket.send(message);
		});
		
		socket.on('close', function() { 
			console.log('Client disconnected!');
		});
	});
	
	server.on('connection', function (socket) {	
		console.log('Client connected!!');
	});
});