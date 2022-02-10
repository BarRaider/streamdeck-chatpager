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
    showHideLoadFromFiles("none");
    showHideLoadFromPI("");

    if (payload['loadFromFile']) {
        showHideLoadFromFiles("");
        showHideLoadFromPI("none");
    }

}

function showHideLoadFromFiles(displayValue) {
    var dvLoadFromFiles = document.getElementById('dvLoadFromFiles');
    dvLoadFromFiles.style.display = displayValue;
}

function showHideLoadFromPI(displayValue) {
    var dvLoadFromPI = document.getElementById('dvLoadFromPI');
    dvLoadFromPI.style.display = displayValue;
}