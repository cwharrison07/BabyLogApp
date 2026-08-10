var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
var app = builder.Build();

app.UseSession();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/newLog", (HttpContext context) =>
{
    if (context.Session.GetString("OliverSleep") == null)
    {
        context.Session.SetString("OliverSleep", "Fell Asleep");
        context.Session.SetString("IslaSleep", "Fell Asleep");
    }

    return Results.Content(
    $$"""
    <html>
    <head>
        <link rel="stylesheet" href="style.css">
    </head>
    <body>
        <div class="log-labels">
            <div class="oliver-label">
                <img src="/images/Oliver-Head.png" alt="Oliver img">
                <label>Oliver</label>
            </div>
            <div class="isla-label">
                <img src="/images/Isla-Head.png" alt="Isla img">
                <label>Isla</label>
            </div>
        </div>
        <div class="page">
            <div id="eventLogOliver" class="event-log"></div>
            <div class="log-input">
                <div class="input-group">
                    <div class="slider-container">
                        <span>Oliver</span>
                        <input type="range" id="toggle" min="0" max="1" step="1" value="0">
                        <span>Isla</span>
                    </div>

                    <button id="sleep" onclick="eventInput(this)">Fell Asleep</button>

                    <div class="diaper-row">
                        <button id="pee" onclick="eventInput(this)">Changed Diaper (Pee)</button>
                        <button id="poop" onclick="eventInput(this)">Changed Diaper (Poop)</button>
                    </div>

                    <div class="eat-row">
                        <label>Ate: </label>
                        <input id="eat" onkeydown="if (event.key === 'Enter') eventInput(this)" placeholder="Amount">
                    </div>

                    <div class="slider-container">
                        <span>Current</span>
                        <input type="range" id="toggleTime" min="0" max="1" step="1" value="0">
                        <span>Past</span>
                    </div>

                    <div id="customTime" class="custom-time"></div>
                </div>
            </div>
            <div id="eventLogIsla" class="event-log"></div>
        </div>
        <script>
            let timeSlider = document.getElementById("toggleTime");

            timeSlider.addEventListener("change", function() {
                let timeHTML = document.getElementById("customTime");
                if (timeSlider.value == 0) {
                    timeHTML.innerHTML = "";
                } else {
                    let timeInput = document.createElement("input");
                    timeInput.placeholder = "Time of Event";
                    timeInput.id = "timeInput";
                    timeHTML.append(timeInput);
                }
            });

            let oliverSleep = "{{context.Session.GetString("OliverSleep")}}";
            let islaSleep = "{{context.Session.GetString("IslaSleep")}}";
            
            let sleepButton = document.getElementById("sleep");
            let slider = document.getElementById("toggle");

            slider.addEventListener("change", function() {
                if (slider.value == 0) {
                    sleepButton.textContent = oliverSleep;
                } else {
                    sleepButton.textContent = islaSleep;
                }
            });

            function eventInput(input) {
                let eventLogHTML = document.getElementById("eventLogOliver");
                if (slider.value == 1) eventLogHTML = document.getElementById("eventLogIsla");
                let time;
                if (timeSlider.value == 0) {
                    time = new Date().toLocaleTimeString([], {
                        hour: "numeric",
                        minute: "2-digit"
                    });
                } else {
                    let timeInput = document.getElementById("timeInput");
                    if (timeInput.value !== "") time = timeInput.value;
                    else {
                        time = new Date().toLocaleTimeString([], {
                            hour: "numeric",
                            minute: "2-digit"
                        });
                    }
                    timeInput.value = "";
                }
                let eventLog = document.createElement("label");
                if (input.id === "sleep") {
                    let name = "Oliver";
                    if (slider.value == 1) name = "Isla";

                    if (input.textContent === "Fell Asleep") {
                        fetch(`/setSleep?name=${name}&status=Woke%20Up`);
                        input.textContent = "Woke Up";

                        if (name === "Oliver") oliverSleep = "Woke Up";
                        else islaSleep = "Woke Up";

                        eventLog.textContent = time + ": Fell Asleep";
                        eventLogHTML.appendChild(eventLog);
                    } else {
                        fetch(`/setSleep?name=${name}&status=Fell%20Asleep`);
                        input.textContent = "Fell Asleep";

                        if (name === "Oliver") oliverSleep = "Fell Asleep";
                        else islaSleep = "Fell Asleep";

                        eventLog.textContent = time + ": Woke Up";
                        eventLogHTML.appendChild(eventLog);
                    }
                } else if (input.id === "eat") {
                    eventLog.textContent = time + ": Ate " + input.value;
                    input.value = "";
                    eventLogHTML.appendChild(eventLog);
                } else if (input.id === "pee") {
                    eventLog.textContent = time + ": Changed Diaper (Pee)";
                    eventLogHTML.appendChild(eventLog);
                } else {
                    eventLog.textContent = time + ": Changed Diaper (Poop)";
                    eventLogHTML.appendChild(eventLog); 
                }
            }
        </script>
    </body>
    </html>
    """,
    "text/html");
});

app.MapGet("/setSleep", (HttpContext context, string name, string status) =>
{
    if (name == "Oliver")
    {
        context.Session.SetString("OliverSleep", status);
    }
    else if (name == "Isla")
    {
        context.Session.SetString("IslaSleep", status);
    }

    return Results.Ok();
});

app.Run();
