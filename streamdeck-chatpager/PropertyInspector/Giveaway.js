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
    setAutoDrawSettings("none");
    if (payload['autoDraw']) {
        setAutoDrawSettings("");
    }
}

function chooseWinner() {
    var payload = {};
    payload.property_inspector = 'chooseWinner';
    sendPayloadToPlugin(payload);
}

function endGiveaway() {
    var payload = {};
    payload.property_inspector = 'endGiveaway';
    sendPayloadToPlugin(payload);
}

function openSaveFilePicker(title, filter, propertyName) {
    console.log("openSaveFilePicker called: ", title, filter, propertyName);
    var payload = {};
    payload.property_inspector = 'loadsavepicker';
    payload.picker_title = title;
    payload.picker_filter = filter;
    payload.property_name = propertyName;
    sendPayloadToPlugin(payload);
}

function setAutoDrawSettings(displayValue) {
    var dvAutoDrawSettings = document.getElementById('dvAutoDrawSettings');
    dvAutoDrawSettings.style.display = displayValue;
}