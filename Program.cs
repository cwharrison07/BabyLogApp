using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
var app = builder.Build();

app.UseSession();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/newLog", (HttpContext context) =>
{
    context.Session.SetString("OliverSleep", "Fell Asleep");
    context.Session.SetString("IslaSleep", "Fell Asleep");

    context.Session.SetInt32("OliverNapCount", 0);
    context.Session.SetInt32("IslaNapCount", 0);
    context.Session.SetInt32("OliverAmountEaten", 0);
    context.Session.SetInt32("IslaAmountEaten", 0);
    context.Session.SetInt32("OliverPoopCount", 0);
    context.Session.SetInt32("IslaPoopCount", 0);
    context.Session.SetInt32("OliverNapTime", 0);
    context.Session.SetInt32("IslaNapTime", 0);

    return Results.Content(
    $$"""
    <html>
    <head>
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <link rel="stylesheet" href="style.css">
    </head>
    <body>
        <div class="page">
            <div class="log-section">
                <div class="oliver-label">
                    <img src="/images/Oliver-Head.png" alt="Oliver img">
                    <label>Oliver</label>
                </div>

                <div id="eventLogOliver" class="event-log"></div>
            </div>

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

            <div class="log-section">
                <div class="isla-label">
                    <img src="/images/Isla-Head.png" alt="Isla img">
                    <label>Isla</label>
                </div>

                <div id="eventLogIsla" class="event-log"></div>
            </div>

            <div class="stats">
                <label id="oliver-stat-label">Oliver</label>
                <label id="isla-stat-label">Isla</label>
                <label class="stat-title"># of Naps:</label>
                <label id="oliver-nap-count">{{context.Session.GetInt32("OliverNapCount")}}</label>
                <label id="isla-nap-count">{{context.Session.GetInt32("IslaNapCount")}}</label>
                <label class="stat-title">Time Asleep:</label>
                <label id="oliver-nap-time">{{context.Session.GetInt32("OliverNapTime")}}</label>
                <label id="isla-nap-time">{{context.Session.GetInt32("IslaNapTime")}}</label>
                <label class="stat-title">Amount Eaten:</label>
                <label id="oliver-amount-eaten">{{context.Session.GetInt32("OliverAmountEaten")}}</label>
                <label id="isla-amount-eaten">{{context.Session.GetInt32("IslaAmountEaten")}}</label>
                <label class="stat-title"># of Poops:</label>
                <label id="oliver-poop-count">{{context.Session.GetInt32("OliverPoopCount")}}</label>
                <label id="isla-poop-count">{{context.Session.GetInt32("IslaPoopCount")}}</label>
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
                let name = "Oliver";
                if (slider.value == 1) name = "Isla";

                let eventLog = document.createElement("label");
                if (input.id === "sleep") {
                    

                    if (input.textContent === "Fell Asleep") {
                        fetch(`/setSleep?name=${name}&status=Woke%20Up&time=${time}`)
                            .then(response => response.json())
                            .then(data => {
                                timeAsleep = minutesToHHMM(data.napTime);
                                if (data.name === "Oliver") {
                                    document.getElementById("oliver-nap-count").textContent = data.napCount;
                                    document.getElementById("oliver-nap-time").textContent = timeAsleep;
                                } else {
                                    document.getElementById("isla-nap-count").textContent = data.napCount;
                                    document.getElementById("isla-nap-time").textContent = timeAsleep;
                                }
                            });
                        input.textContent = "Woke Up";

                        if (name === "Oliver") oliverSleep = "Woke Up";
                        else islaSleep = "Woke Up";

                        eventLog.textContent = time + ": Fell Asleep";
                        eventLogHTML.appendChild(eventLog);
                    } else {
                        fetch(`/setSleep?name=${name}&status=Fell%20Asleep&time=${time}`)
                            .then(response => response.json())
                            .then(data => {
                                timeAsleep = minutesToHHMM(data.napTime);
                                if (data.name === "Oliver") {
                                    document.getElementById("oliver-nap-count").textContent = data.napCount;
                                    document.getElementById("oliver-nap-time").textContent = timeAsleep;
                                } else {
                                    document.getElementById("isla-nap-count").textContent = data.napCount;
                                    document.getElementById("isla-nap-time").textContent = timeAsleep;
                                }
                            });
                        input.textContent = "Fell Asleep";

                        if (name === "Oliver") oliverSleep = "Fell Asleep";
                        else islaSleep = "Fell Asleep";

                        eventLog.textContent = time + ": Woke Up";
                        eventLogHTML.appendChild(eventLog);
                    }
                } else if (input.id === "eat") {
                    fetch(`/updateIntake?name=${name}&amount=${input.value.substring(0, 1)}`)
                        .then(response => response.json())
                        .then(data => {
                            if (data.name === "Oliver") {
                                document.getElementById("oliver-amount-eaten").textContent = data.amountEaten;
                            } else {
                                document.getElementById("isla-amount-eaten").textContent = data.amountEaten;
                            }
                        });
                    eventLog.textContent = time + ": Ate " + input.value;
                    input.value = "";
                    eventLogHTML.appendChild(eventLog);
                } else if (input.id === "pee") {
                    eventLog.textContent = time + ": Changed Diaper (Pee)";
                    eventLogHTML.appendChild(eventLog);
                } else {
                    fetch(`/updateOutput?name=${name}`)
                        .then(response => response.json())
                        .then(data => {
                            if (data.name === "Oliver") {
                                document.getElementById("oliver-poop-count").textContent = data.poopCount;
                            } else {
                                document.getElementById("isla-poop-count").textContent = data.poopCount;
                            }
                        });
                    eventLog.textContent = time + ": Changed Diaper (Poop)";
                    eventLogHTML.appendChild(eventLog); 
                }
            }

            function minutesToHHMM(totalMinutes) {
                let hours = Math.floor(totalMinutes / 60);
                let minutes = totalMinutes % 60;

                return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}`;
            }
        </script>
    </body>
    </html>
    """,
    "text/html");
});

app.MapGet("/setSleep", (HttpContext context, string name, string status, string time) =>
{
    if (name == "Oliver")
    {
        context.Session.SetString("OliverSleep", status);
    }
    else if (name == "Isla")
    {
        context.Session.SetString("IslaSleep", status);
    }

    int hour = int.Parse(time.Substring(0, time.IndexOf(":"))) % 12;
    int min = int.Parse(time.Substring(time.IndexOf(":") + 1, 2));
    int timeInMin = 60 * hour + min;

    if (time.Contains("PM", StringComparison.OrdinalIgnoreCase)) timeInMin += 720;

    if (status == "Woke Up")
    {
        
        context.Session.SetInt32($"{name}NapCount", (int)context.Session.GetInt32($"{name}NapCount")! + 1);
        context.Session.SetInt32($"{name}StartSleep", timeInMin);
    } else
    {
        context.Session.SetInt32($"{name}NapTime", (int)context.Session.GetInt32($"{name}NapTime")! + timeInMin - (int)context.Session.GetInt32($"{name}StartSleep")!);
    }

    return Results.Json(new
    {
        name = name,
        napCount = context.Session.GetInt32($"{name}NapCount"),
        napTime = context.Session.GetInt32($"{name}NapTime")
    });
});

app.MapGet("/updateIntake", (HttpContext context, string name, int amount) =>
{
    int current = context.Session.GetInt32($"{name}AmountEaten") ?? 0;
    int newAmount = current + amount;

    context.Session.SetInt32($"{name}AmountEaten", newAmount);

    return Results.Json(new
    {
        name = name,
        amountEaten = newAmount
    });
});

app.MapGet("/updateOutput", (HttpContext context, string name) =>
{
    int current = context.Session.GetInt32($"{name}PoopCount") ?? 0;
    int newCount = current + 1;

    context.Session.SetInt32($"{name}PoopCount", newCount);

    return Results.Json(new
    {
        name = name,
        poopCount = newCount
    });
});


app.Run();
