var engine = require('engine.io');
var http = require('http').createServer();
var server = engine.attach(http);

var port = 1009;

http.listen(port, function() {
	console.log('Listening on ' + port);
	
	server.on('connection', function (socket) {	
		console.log('Client connected!');
		socket.send(Buffer.from([0, 1, 2, 3, 4, 5]));
		socket.send('hello world');
		socket.send(124123);
		
		socket.send(JSON.stringify({'test': 'test'}));
		socket.send({'test': 'test'});
		
		socket.send(JSON.stringify([{'test1': 'test1'},{'test2': 'test2'}]));
		socket.send([{'test1': 'test1'},{'test2': 'test2'}]);
		
		socket.on('message', function (message) { 
			console.log('Client : ' + message);
			
			socket.send(message);
		});
		
		socket.on('close', function() { 
			console.log('Client disconnected!');
		});
		
		socket.on('error', function (data) {
			console.log(data);
			console.log('Client disconnected!!');
		});
	});
});