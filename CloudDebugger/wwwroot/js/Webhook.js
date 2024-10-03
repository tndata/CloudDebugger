"use strict";

//SignalR connection script, used by the Webhook tool.

var connection = new signalR.HubConnectionBuilder().withUrl("/WebHookHub").build();

connection.on("ReceiveMessage1", function (color, content) {
    let li1 = document.createElement("span");
    document.getElementById("Messagebox1").appendChild(li1);
    li1.style.color = color;
    li1.textContent = `${content} `;
});
connection.on("ReceiveMessage2", function (color, content) {
    let li2 = document.createElement("span");
    document.getElementById("Messagebox2").appendChild(li2);
    li2.style.color = color;
    li2.textContent = `${content} `;
});
connection.on("ReceiveMessage3", function (color, content) {
    let li3 = document.createElement("span");
    document.getElementById("Messagebox3").appendChild(li3);
    li3.style.color = color;
    li3.textContent = `${content} `;
});
connection.on("ReceiveMessage4", function (color, content) {
    let li4 = document.createElement("span");
    document.getElementById("Messagebox4").appendChild(li4);
    li4.style.color = color;
    li4.textContent = `${content}`;
});

// Start the SignalR connection
connection.start().then(function () {
    // Connection started
}).catch(function (err) {
    return console.error(err.toString());
});

