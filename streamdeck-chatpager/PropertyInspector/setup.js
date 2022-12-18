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
            checkStatus(payload);
        }
        else if (jsonObj.event === 'didReceiveSettings') {
            var payload = jsonObj.payload;
            checkToken(payload.settings);
        }
    });
});

function checkStatus(payload) {
    console.log("Received status update...");
    if (!authWindow) {
        console.log("authWindow does not exist, exiting");
        return;
    }

    if (payload['PONG']) {
        let status = payload['PONG']['datetime'];
        console.log("Got PONG", status);
        authWindow.gotPong();
    }
}

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
                'url': 'https://id.twitch.tv/oauth2/authorize?client_id=o02y5mq522n0qaphd6yasbhyfplye3&redirect_uri=https://barraider.com/twitchredir&response_type=token&scope=chat:read%20chat:edit%20whispers:read%20whispers:edit%20clips:edit%20channel:moderate%20channel:manage:videos%20user:read:follows%20user:edit:broadcast%20bits:read%20channel:read:subscriptions%20channel:read:redemptions%20channel:manage:broadcast%20channel:edit:commercial%20moderator:manage:shield_mode'
            }
        };
        websocket.send(JSON.stringify(json));
    }
}

function openTwitter() {
    if (websocket && (websocket.readyState === 1)) {
        const json = {
            'event': 'openUrl',
            'payload': {
                'url': 'https://buz.bz/barT'
            }
        };
        websocket.send(JSON.stringify(json));
    }
}

function openDiscord() {
    if (websocket && (websocket.readyState === 1)) {
        const json = {
            'event': 'openUrl',
            'payload': {
                'url': 'https://buz.bz/d'
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

function sendPing() {
    console.log("Sending Ping");

    var payload = {};
    payload.property_inspector = 'PING';
    sendPayloadToPlugin(payload);
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
