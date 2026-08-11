using Microsoft.Data.Sqlite;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

string connectionString = "Data Source=babylog.db";

using var connection = new SqliteConnection(connectionString);
connection.Open();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
var app = builder.Build();

app.UseSession();
app.UseDefaultFiles();
app.UseStaticFiles();

using var command = connection.CreateCommand();

command.CommandText = """
    CREATE TABLE IF NOT EXISTS Logs (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Baby TEXT NOT NULL,
        EventType TEXT NOT NULL,
        Amount INTEGER,
        EventTime TEXT NOT NULL
    );
    """;

command.ExecuteNonQuery();

command.CommandText = """
    CREATE TABLE IF NOT EXISTS Stats (
        Baby TEXT PRIMARY KEY,
        NapCount INTEGER NOT NULL DEFAULT 0,
        NapTime INTEGER NOT NULL DEFAULT 0,
        AmountEaten INTEGER NOT NULL DEFAULT 0,
        PoopCount INTEGER NOT NULL DEFAULT 0,
        SleepStatus TEXT NOT NULL DEFAULT 'Fell Asleep',
        StartSleep INTEGER
    );

    INSERT OR IGNORE INTO Stats (Baby)
    VALUES ('Oliver'), ('Isla');
    """;

command.ExecuteNonQuery();

app.MapGet("/newLog", (HttpContext context) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    var command = connection.CreateCommand();
    command.CommandText = """
    SELECT Baby, NapCount, NapTime, AmountEaten, PoopCount, SleepStatus
    FROM Stats
    """;

    int oliverNapCount = 0;
    int oliverNapTime = 0;
    int oliverAmountEaten = 0;
    int oliverPoopCount = 0;
    string oliverSleepStatus = "Fell Asleep";

    int islaNapCount = 0;
    int islaNapTime = 0;
    int islaAmountEaten = 0;
    int islaPoopCount = 0;
    string islaSleepStatus = "Fell Asleep";

    using (var reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            string baby = reader.GetString(0);

            if (baby == "Oliver")
            {
                oliverNapCount = reader.GetInt32(1);
                oliverNapTime = reader.GetInt32(2);
                oliverAmountEaten = reader.GetInt32(3);
                oliverPoopCount = reader.GetInt32(4);
                oliverSleepStatus = reader.GetString(5);
            }
            else if (baby == "Isla")
            {
                islaNapCount = reader.GetInt32(1);
                islaNapTime = reader.GetInt32(2);
                islaAmountEaten = reader.GetInt32(3);
                islaPoopCount = reader.GetInt32(4);
                islaSleepStatus = reader.GetString(5);
            }
        }
    } // reader is closed here

    command.CommandText = """
        SELECT Baby, EventType, Amount, EventTime
        FROM Logs
        ORDER BY Id
        """;

    var oliverLogs = new List<string>();
    var islaLogs = new List<string>();

    using (var reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            string baby = reader.GetString(0);
            string eventType = reader.GetString(1);
            int? amount = reader.IsDBNull(2) ? null : reader.GetInt32(2);
            string eventTime = reader.GetString(3);

            string log;

            if (eventType == "Ate")
            {
                log = $"{eventTime}: Ate {amount}";
            }
            else if (eventType == "Pee")
            {
                log = $"{eventTime}: Changed Diaper (Pee)";
            }
            else if (eventType == "Poop")
            {
                log = $"{eventTime}: Changed Diaper (Poop)";
            }
            else
            {
                log = $"{eventTime}: {eventType}";
            }

            if (baby == "Oliver")
                oliverLogs.Add(log);
            else if (baby == "Isla")
                islaLogs.Add(log);
        }
    }

    string oliverLogHtml = "";

    foreach (string log in oliverLogs)
    {
        oliverLogHtml += $"<label>{log}</label>";
    }

    string islaLogHtml = "";

    foreach (string log in islaLogs)
    {
        islaLogHtml += $"<label>{log}</label>";
    }

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

                <div id="eventLogOliver" class="event-log">{{oliverLogHtml}}</div>
            </div>

            <div class="log-input">
                <div class="input-group">
                    <div class="slider-container">
                        <span>Oliver</span>
                        <input type="range" id="toggle" min="0" max="1" step="1" value="0">
                        <span>Isla</span>
                    </div>

                    <button id="sleep" onclick="eventInput(this)">{{oliverSleepStatus}}</button>

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

                <div id="eventLogIsla" class="event-log">{{islaLogHtml}}</div>
            </div>

            <div class="stats">
                <label id="oliver-stat-label">Oliver</label>
                <label id="isla-stat-label">Isla</label>
                <label class="stat-title"># of Naps:</label>
                <label id="oliver-nap-count">{{oliverNapCount}}</label>
                <label id="isla-nap-count">{{islaNapCount}}</label>
                <label class="stat-title">Time Asleep:</label>
                <label id="oliver-nap-time"></label>
                <label id="isla-nap-time"></label>
                <label class="stat-title">Amount Eaten:</label>
                <label id="oliver-amount-eaten">{{oliverAmountEaten}}</label>
                <label id="isla-amount-eaten">{{islaAmountEaten}}</label>
                <label class="stat-title"># of Poops:</label>
                <label id="oliver-poop-count">{{oliverPoopCount}}</label>
                <label id="isla-poop-count">{{islaPoopCount}}</label>
            </div>

            <div class="reset-log">
                <button id="reset-button" onclick="resetLogs()">Reset Logs</button>
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

            let oliverSleep = "{{oliverSleepStatus}}";
            let islaSleep = "{{islaSleepStatus}}";
            
            let sleepButton = document.getElementById("sleep");
            let slider = document.getElementById("toggle");

            slider.addEventListener("change", function() {
                if (slider.value == 0) {
                    sleepButton.textContent = oliverSleep;
                } else {
                    sleepButton.textContent = islaSleep;
                }
            });

            document.getElementById("oliver-nap-time").textContent =
                minutesToHHMM({{oliverNapTime}});

            document.getElementById("isla-nap-time").textContent =
                minutesToHHMM({{islaNapTime}});

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
                                let timeAsleep = minutesToHHMM(data.napTime);
                                if (data.name === "Oliver") {
                                    document.getElementById("oliver-nap-count").textContent = data.napCount;
                                    document.getElementById("oliver-nap-time").textContent = timeAsleep;
                                } else {
                                    document.getElementById("isla-nap-count").textContent = data.napCount;
                                    document.getElementById("isla-nap-time").textContent = timeAsleep;
                                }
                            });
                        fetch(`/addLog?name=${name}&eventType=Fell%20Asleep&eventTime=${encodeURIComponent(time)}`);
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
                        fetch(`/addLog?name=${name}&eventType=Woke%20Up&eventTime=${encodeURIComponent(time)}`);
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
                    fetch(`/addLog?name=${name}&eventType=Ate&amount=${input.value.substring(0, 1)}&eventTime=${encodeURIComponent(time)}`);
                    eventLog.textContent = time + ": Ate " + input.value;
                    input.value = "";
                    eventLogHTML.appendChild(eventLog);
                } else if (input.id === "pee") {
                    fetch(`/addLog?name=${name}&eventType=Pee&eventTime=${encodeURIComponent(time)}`);
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
                    fetch(`/addLog?name=${name}&eventType=Poop&eventTime=${encodeURIComponent(time)}`);
                    eventLog.textContent = time + ": Changed Diaper (Poop)";
                    eventLogHTML.appendChild(eventLog); 
                }
            }

            function minutesToHHMM(totalMinutes) {
                let hours = Math.floor(totalMinutes / 60);
                let minutes = totalMinutes % 60;

                return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}`;
            }

            function resetLogs() {
                fetch(`/resetLogs`);
                document.getElementById("eventLogOliver").innerHTML = "";
                document.getElementById("eventLogIsla").innerHTML = "";
                document.getElementById("sleep").textContent = "Fell Asleep";
                document.getElementById("oliver-nap-count").textContent = 0;
                document.getElementById("isla-nap-count").textContent = 0;
                document.getElementById("oliver-nap-time").textContent = "00:00";
                document.getElementById("isla-nap-time").textContent = "00:00";
                document.getElementById("oliver-amount-eaten").textContent = 0;
                document.getElementById("isla-amount-eaten").textContent = 0;
                document.getElementById("oliver-poop-count").textContent = 0;
                document.getElementById("isla-poop-count").textContent = 0;
            }
        </script>
    </body>
    </html>
    """,
    "text/html");
});

app.MapGet("/setSleep", (HttpContext context, string name, string status, string time) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var command = connection.CreateCommand();

    command.CommandText =
        """
        UPDATE Stats
        SET SleepStatus = $status
        WHERE Baby = $name
        """;

    command.Parameters.AddWithValue("$status", status);
    command.Parameters.AddWithValue("$name", name);

    command.ExecuteNonQuery();

    int hour = int.Parse(time.Substring(0, time.IndexOf(":"))) % 12;
    int min = int.Parse(time.Substring(time.IndexOf(":") + 1, 2));
    int timeInMin = 60 * hour + min;

    if (time.Contains("PM", StringComparison.OrdinalIgnoreCase)) timeInMin += 720;

    if (status == "Woke Up")
    {

        command.CommandText = """
            UPDATE Stats
            SET NapCount = NapCount + 1,
                StartSleep = $startSleep
            WHERE Baby = $name
            """;
        command.Parameters.Clear();
        command.Parameters.AddWithValue("$startSleep", timeInMin);
        command.Parameters.AddWithValue("$name", name);

        command.ExecuteNonQuery();
    } else
    {
        command.CommandText = """
            UPDATE Stats
            SET NapTime = NapTime + $timeInMin - StartSleep
            WHERE Baby = $name
            """;

        command.Parameters.Clear();
        command.Parameters.AddWithValue("$timeInMin", timeInMin);
        command.Parameters.AddWithValue("$name", name);

        command.ExecuteNonQuery();
    }

    command.CommandText = """
    SELECT NapCount, NapTime
    FROM Stats
    WHERE Baby = $name
    """;

    command.Parameters.Clear();
    command.Parameters.AddWithValue("$name", name);

    using var reader = command.ExecuteReader();

    if (reader.Read())
    {
        return Results.Json(new
        {
            name = name,
            napCount = reader.GetInt32(0),
            napTime = reader.GetInt32(1)
        });
    }

    return Results.NotFound();
});

app.MapGet("/updateIntake", (HttpContext context, string name, int amount) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var command = connection.CreateCommand();

    command.CommandText =
        """
        UPDATE Stats
        SET AmountEaten = AmountEaten + $amount
        WHERE Baby = $name
        """;

    command.Parameters.Clear();
    command.Parameters.AddWithValue("$amount", amount);
    command.Parameters.AddWithValue("$name", name);

    command.ExecuteNonQuery();

    command.CommandText = """
    SELECT AmountEaten
    FROM Stats
    WHERE Baby = $name
    """;

    command.Parameters.Clear();
    command.Parameters.AddWithValue("$name", name);

    using var reader = command.ExecuteReader();

    if (reader.Read())
    {
        return Results.Json(new
        {
            name = name,
            amountEaten = reader.GetInt32(0)
        });
    }

    return Results.NotFound();
});

app.MapGet("/updateOutput", (HttpContext context, string name) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var command = connection.CreateCommand();

    command.CommandText =
        """
        UPDATE Stats
        SET PoopCount = PoopCount + 1
        WHERE Baby = $name
        """;

    command.Parameters.Clear();
    command.Parameters.AddWithValue("$name", name);

    command.ExecuteNonQuery();

    command.CommandText = """
    SELECT PoopCount
    FROM Stats
    WHERE Baby = $name
    """;

    command.Parameters.Clear();
    command.Parameters.AddWithValue("$name", name);

    using var reader = command.ExecuteReader();

    if (reader.Read())
    {
        return Results.Json(new
        {
            name = name,
            poopCount = reader.GetInt32(0)
        });
    }

    return Results.NotFound();
});

app.MapGet("/resetLogs", (HttpContext context) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var command = connection.CreateCommand();

    command.CommandText = """
        UPDATE Stats
        SET SleepStatus = 'Fell Asleep',
            NapCount = 0,
            NapTime = 0,
            AmountEaten = 0,
            PoopCount = 0,
            StartSleep = NULL
        WHERE Baby IN ('Oliver', 'Isla')
        """;

    command.ExecuteNonQuery();

    command.CommandText = """
        DELETE FROM Logs
        """;

    command.ExecuteNonQuery();

    return Results.Ok();
});

app.MapGet("/addLog", (string name, string eventType, int? amount, string eventTime) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var command = connection.CreateCommand();

    command.CommandText = """
        INSERT INTO Logs (Baby, EventType, Amount, EventTime)
        VALUES ($name, $eventType, $amount, $eventTime)
        """;

    command.Parameters.AddWithValue("$name", name);
    command.Parameters.AddWithValue("$eventType", eventType);
    command.Parameters.AddWithValue("$amount", (object?)amount ?? DBNull.Value);
    command.Parameters.AddWithValue("$eventTime", eventTime);

    command.ExecuteNonQuery();

    return Results.Ok();
});


app.Run();
