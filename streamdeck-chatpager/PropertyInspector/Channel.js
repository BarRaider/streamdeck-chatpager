document.addEventListener('websocketCreate', function () {
    console.log("Websocket created!");
    checkSettings(actionInfo.payload.settings);

    websocket.addEventListener('message', function (event) {
        console.log("Got message event!");

        // Received message from Stream Deck
        var jsonObj = JSON.parse(event.data);

        if (jsonObj.event === 'didReceiveSettings') {
            var payload = jsonObj.payload;
            checkSettings(payload.settings);
        }
    });
});

function checkSettings(payload) {
    console.log("Checking Settings");

    setSoundOnLiveSettings("none");
    if (payload['playSoundOnLive']) {
        setSoundOnLiveSettings("");
    }
}

function setSoundOnLiveSettings(displayValue) {
    var dvSoundOnLiveSettings = document.getElementById('dvSoundOnLiveSettings');
    dvSoundOnLiveSettings.style.display = displayValue;
}