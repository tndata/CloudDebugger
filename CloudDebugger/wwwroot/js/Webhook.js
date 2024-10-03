"use strict";

//SignalR connection script, used by the Webhook tool.

var connection = new signalR.HubConnectionBuilder().withUrl("/WebHookHub").build();

connection.on("ReceiveMessage1", function (color, content) {
    let li = document.createElement("span");
    document.getElementById("Messagebox1").appendChild(li);
    li.style.color = color;
    li.textContent = `${content} `;
});
connection.on("ReceiveMessage2", function (color, content) {
    let li = document.createElement("span");
    document.getElementById("Messagebox2").appendChild(li);
    li.style.color = color;
    li.textContent = `${content} `;
});
connection.on("ReceiveMessage3", function (color, content) {
    let li = document.createElement("span");
    document.getElementById("Messagebox3").appendChild(li);
    li.style.color = color;
    li.textContent = `${content} `;
});
connection.on("ReceiveMessage4", function (color, content) {
    let li = document.createElement("span");
    document.getElementById("Messagebox4").appendChild(li);
    li.style.color = color;
    li.textContent = `${content}`;
});

// Start the SignalR connection
connection.start().then(function () {
    // Connection started
}).catch(function (err) {
    return console.error(err.toString());
});

