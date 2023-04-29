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

    setKeypressSettings("none");
    setKeypressSubOptions(false);
    if (payload['customBrowser']) {
        setKeypressSettings("");

        if (payload['browserExecutableFile'].trim() !== '') {
            setKeypressSubOptions(true);
        }
    }
}

function setSoundOnLiveSettings(displayValue) {
    var dvSoundOnLiveSettings = document.getElementById('dvSoundOnLiveSettings');
    dvSoundOnLiveSettings.style.display = displayValue;
}

function setKeypressSettings(displayValue) {
    var dvKeypressSettings = document.getElementById('dvKeypressSettings');
    dvKeypressSettings.style.display = displayValue;
}

function setKeypressSubOptions(enabled) {
    var keypressNewWindow = document.getElementById('keypressNewWindow');
    var keypressAppMode = document.getElementById('keypressAppMode');
    keypressNewWindow.disabled = !enabled;
    keypressAppMode.disabled = !enabled;
}