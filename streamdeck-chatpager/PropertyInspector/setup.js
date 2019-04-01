var authWindow = null;

document.addEventListener('websocketCreate', function () {
    console.log("Websocket created!");
    checkToken(actionInfo.payload.settings);

    websocket.addEventListener('message', function (event) {
        console.log("Got message event!");

        // Received message from Stream Deck
        var jsonObj = JSON.parse(event.data);

        if (jsonObj.event === 'sendToPropertyInspector') {
            var payload = jsonObj.payload;
            checkToken(payload);
        }
        else if (jsonObj.event === 'didReceiveSettings') {
            var payload = jsonObj.payload;
            checkToken(payload.settings);
        }
    });
});

function checkToken(payload) {
    console.log("Checking Token...");
    var tokenExists = document.getElementById('tokenExists');
    tokenExists.value = payload['tokenExists'];

    if (payload['tokenExists']) {
        setSettingsWrapper("");
        var event = new Event('tokenExists');
        document.dispatchEvent(event);

        if (authWindow) {
            authWindow.loadSuccessView();
        }
    }
    else {
        setSettingsWrapper("none");
        if (authWindow) {
            authWindow.loadFailedView();
        }
        else {
            authWindow = window.open("Setup/index.html")
        }
    }
}

function setSettingsWrapper(displayValue) {
    var sdWrapper = document.getElementById('sdWrapper');
    sdWrapper.style.display = displayValue;
}

function resetPlugin() {
    var payload = {};
    payload.property_inspector = 'resetPlugin';
    sendPayloadToPlugin(payload);
}

function openTwitchAuth() {
    if (websocket && (websocket.readyState === 1)) {
        const json = {
            'event': 'openUrl',
            'payload': {
                'url': 'https://id.twitch.tv/oauth2/authorize?client_id=aao2j8zfthy2rh10satdmxwojt70ph&redirect_uri=https://barraider.github.io/twitchauth&response_type=token&scope=channel_feed_read%20chat:read%20chat:edit%20whispers:read%20whispers:edit'
            }
        };
        websocket.send(JSON.stringify(json));
    }
}

function updateApprovalCode(val) {
    var approvalCode = val;

    var payload = {};
    payload.property_inspector = 'updateApproval';
    payload.approvalCode = approvalCode;
    sendPayloadToPlugin(payload);
    console.log("Approving code");
}

function sendPayloadToPlugin(payload) {
    if (websocket && (websocket.readyState === 1)) {
        const json = {
            'action': actionInfo['action'],
            'event': 'sendToPlugin',
            'context': uuid,
            'payload': payload
        };
        websocket.send(JSON.stringify(json));
    }
}
