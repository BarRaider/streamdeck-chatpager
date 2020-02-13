document.addEventListener('websocketCreate', function () {
    console.log("Websocket created!");
    checkSettings(actionInfo.payload.settings);

    websocket.addEventListener('message', function (event) {
        console.log("Got message event!");

        // Received message from Stream Deck
        var jsonObj = JSON.parse(event.data);

        if (jsonObj.event === 'sendToPropertyInspector') {
            var payload = jsonObj.payload;
            checkSettings(payload);
        }
        else if (jsonObj.event === 'didReceiveSettings') {
            var payload = jsonObj.payload;
            checkSettings(payload.settings);
        }
    });
});

function checkSettings(payload) {
    console.log("Checking Settings");
    showHideFileName("none");
    showHideMessage("");
    if (payload['loadFromFile']) {
        showHideFileName("");
        showHideMessage("none");
    }
}

function showHideFileName(displayValue) {
    var dvFileName = document.getElementById('dvFileName');
    dvFileName.style.display = displayValue;
}

function showHideMessage(displayValue) {
    var dvChatMessage = document.getElementById('dvChatMessage');
    dvChatMessage.style.display = displayValue;
}