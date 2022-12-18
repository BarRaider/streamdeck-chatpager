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

    setDurationSetting("none");
    if (payload['command'] == 2) {
        setDurationSetting("");
    }
    else {
        console.log(payload['command'], payload['command'] == 2, payload['command'] === 2);
    }

}

function setDurationSetting(displayValue) {
    var dvDuration = document.getElementById('dvDuration');
    dvDuration.style.display = displayValue;
}