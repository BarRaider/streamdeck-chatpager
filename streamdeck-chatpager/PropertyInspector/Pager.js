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
    setFullScreenAlert("none");
    setSaveToFile("none");
    setMultipleChannels("none");
    setPubSubNotifications("none");

    if (payload['fullScreenAlert']) {
        setFullScreenAlert("");
    }

    if (payload['saveToFile']) {
        setSaveToFile("");
    }

    if (payload['multipleChannels']) {
        setMultipleChannels("");
    }

    if (payload['pubsubNotifications']) {
        setPubSubNotifications("");
    }
}

function setFullScreenAlert(displayValue) {
    var dvFullScreenAlertSettings = document.getElementById('dvFullScreenAlertSettings');
    dvFullScreenAlertSettings.style.display = displayValue;
}

function setSaveToFile(displayValue) {
    var dvSaveToFile = document.getElementById('dvSaveToFile');
    dvSaveToFile.style.display = displayValue;
}

function setMultipleChannels(displayValue) {
    var dvMultipleChannels = document.getElementById('dvMultipleChannels');
    dvMultipleChannels.style.display = displayValue;
}

function setPubSubNotifications(displayValue) {
    var dvPubsubNotifications = document.getElementById('dvPubsubNotifications');
    dvPubsubNotifications.style.display = displayValue;
}
