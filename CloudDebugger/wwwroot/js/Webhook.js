"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/WebHookHub").build();

connection.on("ReceiveMessage1", function (color, message) {
    var li = document.createElement("span");
    document.getElementById("Messagebox1").appendChild(li);
    li.style.color = color;
    li.textContent = `${message} `;
});
connection.on("ReceiveMessage2", function (color, message) {
    var li = document.createElement("span");
    document.getElementById("Messagebox2").appendChild(li);
    li.style.color = color;
    li.textContent = `${message} `;
});
connection.on("ReceiveMessage3", function (color, message) {
    var li = document.createElement("span");
    document.getElementById("Messagebox3").appendChild(li);
    li.style.color = color;
    li.textContent = `${message} `;
});
connection.on("ReceiveMessage4", function (color, message) {
    var li = document.createElement("span");
    document.getElementById("Messagebox4").appendChild(li);
    li.style.color = color;
    li.textContent = `${message}`;
});



connection.start().then(function () {
    //todo...

}).catch(function (err) {
    return console.error(err.toString());
});

