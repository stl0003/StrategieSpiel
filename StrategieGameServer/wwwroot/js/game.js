// ============================================
// ORIGINALER CODE (angepasst)
// ============================================

let canvas = document.getElementById("myGameArea");
let infoPanel = document.getElementById("myGameArea2");
let menu = document.getElementById("contextMenu");

const GRID_SIZE = 30;
const TILE_SIZE = 24;

const myTiles = [];
let selectedUnit = null;
let myUnits = [];
let myBuildings = [];
let myItems = [];

let hoverTileX = null;
let hoverTileY = null;

let uiCoordY = document.getElementById("ui_coord_y");
let uiCoordX = document.getElementById("ui_coord_x");
let uiUnit = document.getElementById("ui_unit");
let uiAction = document.getElementById("ui_action");
let uiBtnBuild = document.getElementById("ui_btn_build");

// Assets (Pfade mit führendem Slash!)
const assets = {
    tiles: {},
    sources: {
        PLAINS: "/img/kenney_tinybattle/tile_0001.png",
        FOREST: "/img/kenney_tinybattle/tile_0112.png",
        MOUNTAIN: "/img/kenney_tinybattle/tile_0005.png",
        WATER: "/img/kenney_tinybattle/tile_0037.png",
        PIONEER_RED: "/img/kenney_tinybattle/tile_0160.png",
        PIONEER_YELLOW: "/img/kenney_tinybattle/tile_0178.png",
        PIONEER_GREEN: "/img/kenney_tinybattle/tile_0124.png",
        PIONEER_BLUE: "/img/kenney_tinybattle/tile_0142.png",
        CITY_RED: "/img/kenney_tinybattle/tile_0067.png",
        CITY_RED2: "/img/kenney_tinybattle/tile_0064.png",
        CITY_RED3: "/img/kenney_tinybattle/tile_0062.png",
        TRAP: "/img/kenney_tinybattle/tile_0001_trap.png",
        GPS: "/img/kenney_shmup/tile_0003.png",
        EXPLOSION1: "/img/kenney_shmup/tile_0004.png",
        EXPLOSION2: "/img/kenney_shmup/tile_0005.png",
        EXPLOSION3: "/img/kenney_shmup/tile_0006.png",
        BLOP: "/img/kenney_shmup/tile_0007.png",
        WHITE_BLOP: "/img/kenney_shmup/tile_0008.png",
        BOMB1: "/img/kenney_shmup/tile_0012.png",
        BOMB2: "/img/kenney_shmup/tile_0013.png",
        BOMB3: "/img/kenney_shmup/tile_0015.png",
        HEALTH_PACK: "/img/kenney_shmup/tile_0024.png",
        P_PACK: "/img/kenney_shmup/tile_0025.png",
        RANK: "/img/kenney_shmup/tile_0026.png",
        AIM: "/img/kenney_shmup/tile_0027.png",
    },
    loadAll: function (callback) {
        let loadedCount = 0;
        let keys = Object.keys(this.sources);
        for (let key of keys) {
            let img = new Image();
            img.src = this.sources[key];
            img.onload = () => {
                loadedCount++;
                if (loadedCount === keys.length) callback();
            };
            img.onerror = () => {
                loadedCount++;
                if (loadedCount === keys.length) callback();
            };
            this.tiles[key] = img;
        }
    },
};

// ACTIONS (unverändert)
const ACTIONS = {
    move: {
        label: "Move",
        range: 1,
        color: "rgba(255, 255, 0, 0.4)",
        canExecute: function (unit, tile, tx, ty) {
            if (!tile) return false;
            let dx = Math.abs(tx - unit.gridX);
            let dy = Math.abs(ty - unit.gridY);
            if (dx > 1 || dy > 1) return false;
            if (dx === 0 && dy === 0) return false;
            if (tile.type === "WATER") return false;
            let occupied = myUnits.some(
                (u) => u !== unit && u.gridX === tx && u.gridY === ty
            );
            return !occupied;
        },
        execute: function (unit, tile, tx, ty) {
            unit.moveTo(tx, ty);
            if (tile.hasTrap) {
                unit.hp -= 20;
                tile.hasTrap = false;
                if (unit.hp <= 0) {
                    myUnits = myUnits.filter((u) => u !== unit);
                    selectedUnit = null;
                }
            }
            let itemIndex = myItems.findIndex(
                (i) => i.gridX === tx && i.gridY === ty
            );
            if (itemIndex !== -1) {
                let item = myItems[itemIndex];
                if (item.type === "HEALTH_PACK") {
                    unit.hp = Math.min(unit.maxHp, unit.hp + 30);
                    myFloatingTexts.push(
                        new FloatingText(
                            unit.gridX * TILE_SIZE + TILE_SIZE / 2,
                            unit.gridY * TILE_SIZE,
                            "+30 HP",
                            "#00FF00"
                        )
                    );
                }
                let particleColor = item.type === "HEALTH_PACK" ? "#00FF00" : "#FFFF00";
                for (let i = 0; i < 15; i++) {
                    myParticles.push(
                        new Particle(
                            item.x + TILE_SIZE / 2,
                            item.y + TILE_SIZE / 2,
                            particleColor
                        )
                    );
                }
                if (item.type === "HEALTH_PACK") {
                    unit.hp = Math.min(unit.maxHp, unit.hp + 30);
                } else if (item.type === "P_PACK") {
                    console.log("Power pack picked up!");
                }
                myItems.splice(itemIndex, 1);
            }
        },
    },
    attack: {
        label: "Attack",
        range: 1,
        color: "rgba(255, 0, 0, 0.5)",
        canExecute: function (unit, tile, tx, ty) {
            let targetUnit = myUnits.find((u) => u.gridX === tx && u.gridY === ty);
            return targetUnit && targetUnit !== unit;
        },
        execute: function (unit, tile, tx, ty) {
            let targetUnit = myUnits.find((u) => u.gridX === tx && u.gridY === ty);
            if (targetUnit) {
                let damage = 25;
                targetUnit.hp -= damage;
                myFloatingTexts.push(
                    new FloatingText(
                        targetUnit.gridX * TILE_SIZE + TILE_SIZE / 2,
                        targetUnit.gridY * TILE_SIZE,
                        "-" + damage,
                        "#FF0000"
                    )
                );
                console.log(
                    `${unit.nameKey} attacked ${targetUnit.nameKey} for ${damage} HP!`
                );
                if (targetUnit.hp <= 0) {
                    console.log(`${targetUnit.nameKey} was defeated!`);
                    myUnits = myUnits.filter((u) => u !== targetUnit);
                    if (selectedUnit === targetUnit) selectedUnit = null;
                }
            }
        },
    },
    test: {
        label: "Test",
        range: 2,
        color: "rgba(0, 255, 255, 0.4)",
        canExecute: function (unit, tile, tx, ty) {
            if (!tile) return false;
            let dx = Math.abs(tx - unit.gridX);
            let dy = Math.abs(ty - unit.gridY);
            return dx <= 2 && dy <= 2;
        },
        execute: function (unit, tile, tx, ty) {
            console.log("[TEST] tile:", tx, ty, "type=", tile ? tile.type : null);
        },
    },
    placeTrap: {
        label: "Place Trap",
        range: 1,
        color: "rgba(255, 0, 255, 0.35)",
        canExecute: function (unit, tile, tx, ty) {
            if (!tile) return false;
            let dx = Math.abs(tx - unit.gridX);
            let dy = Math.abs(ty - unit.gridY);
            if (dx > 1 || dy > 1) return false;
            if (dx === 0 && dy === 0) return false;
            return true;
        },
        execute: function (unit, tile, tx, ty) {
            tile.hasTrap = true;
            console.log("[TODO] placeTrap at", tx, ty);
        },
    },
};

function rebuildContextMenu() {
    if (!menu) return;
    menu.innerHTML = "";
    Object.keys(ACTIONS).forEach((key) => {
        let action = ACTIONS[key];
        let item = document.createElement("div");
        let iStyle = item.style;
        item.textContent = action.label || key;
        item.style.padding = "4px 8px";
        iStyle.borderStyle = "solid";
        iStyle.borderWidth = "1px";
        iStyle.borderColor = "rgb(0, 0, 0)";
        item.onclick = function () {
            setAction(key);
        };
        menu.appendChild(item);
    });
}

function setAction(type) {
    if (!selectedUnit) {
        menu.style.display = "none";
        return;
    }
    if (!ACTIONS[type]) {
        console.warn("Unknown action:", type);
        menu.style.display = "none";
        return;
    }
    selectedUnit.activeAction = type;
    menu.style.display = "none";
}

var myGameArea = {
    start: function () {
        this.context = canvas.getContext("2d");
        this.interval = setInterval(updateGameArea, 20);
        this.context.imageSmoothingEnabled = false;
    },
    clear: function () {
        this.context.clearRect(0, 0, canvas.width, canvas.height);
        this.context.fillStyle = "#84C669";
        this.context.fillRect(0, 0, canvas.width, canvas.height);
    },
};

function updateInfoPanel() {
    if (!infoPanel) return;
    if (uiCoordY) uiCoordY.textContent = hoverTileY === null ? "-" : String(hoverTileY);
    if (uiCoordX) uiCoordX.textContent = hoverTileX === null ? "-" : String(hoverTileX);
    if (uiUnit) {
        if (selectedUnit) {
            uiUnit.innerHTML = `<strong>${selectedUnit.nameKey}</strong><br>HP: ${selectedUnit.hp}/${selectedUnit.maxHp}`;
        } else {
            uiUnit.textContent = "-";
        }
    }
    if (uiAction) {
        if (selectedUnit && selectedUnit.activeAction) {
            let a = ACTIONS[selectedUnit.activeAction];
            uiAction.textContent = a ? a.label || selectedUnit.activeAction : selectedUnit.activeAction;
        } else {
            uiAction.textContent = "-";
        }
    }
    if (uiAction && selectedUnit) {
        let currentAction = selectedUnit.activeAction;
        document.querySelectorAll(".menuButtons").forEach((btn) => (btn.style.border = "1px solid #999"));
        if (currentAction === "move") document.getElementById("ui_btn_move").style.border = "2px solid yellow";
        if (currentAction === "placeTrap") document.getElementById("ui_btn_build").style.border = "2px solid purple";
        if (currentAction === "attack") document.getElementById("ui_btn_fight").style.border = "2px solid red";
    }
}

if (uiBtnBuild) {
    uiBtnBuild.addEventListener("click", function () {
        console.log("[TODO] Build button clicked (right panel)");
    });
}

function updateGameArea() {
    myGameArea.clear();
    let ctx = myGameArea.context;

    for (let y = 0; y < GRID_SIZE; y++) {
        for (let x = 0; x < GRID_SIZE; x++) {
            let tile = myTiles[y][x];
            myUnits.forEach((u) => {
                if (Math.abs(u.gridX - x) <= 1 && Math.abs(u.gridY - y) <= 1) {
                    tile.explored = true;
                }
            });
            tile.update();
            if (selectedUnit && selectedUnit.activeAction) {
                let action = ACTIONS[selectedUnit.activeAction];
                if (action) {
                    let dx = Math.abs(selectedUnit.gridX - x);
                    let dy = Math.abs(selectedUnit.gridY - y);
                    if (dx <= action.range && dy <= action.range) {
                        ctx.strokeStyle = action.color || "rgba(255,255,255,0.25)";
                        ctx.lineWidth = 2;
                        ctx.strokeRect(tile.x, tile.y, TILE_SIZE, TILE_SIZE);
                    }
                }
            }
        }
    }

    myBuildings.forEach((b) => b.update());

    for (let i = myParticles.length - 1; i >= 0; i--) {
        let p = myParticles[i];
        p.update();
        p.draw(ctx);
        if (p.alpha <= 0) {
            myParticles.splice(i, 1);
        }
    }

    for (let i = myFloatingTexts.length - 1; i >= 0; i--) {
        let ft = myFloatingTexts[i];
        ft.update();
        ft.draw(ctx);
        if (ft.alpha <= 0) {
            myFloatingTexts.splice(i, 1);
        }
    }

    myItems.forEach((item) => item.update());

    if (hoverTileX !== null && hoverTileY !== null) {
        ctx.fillStyle = "rgba(255, 255, 255, 0.3)";
        ctx.strokeStyle = "rgba(255, 255, 255, 0.8)";
        ctx.lineWidth = 2;
        let hX = hoverTileX * TILE_SIZE;
        let hY = hoverTileY * TILE_SIZE;
        ctx.fillRect(hX, hY, TILE_SIZE, TILE_SIZE);
        ctx.strokeRect(hX, hY, TILE_SIZE, TILE_SIZE);
    }

    myUnits.forEach((u) => u.update());
    updateInfoPanel();
}

function spawnRandomItem() {
    let tx = Math.floor(Math.random() * GRID_SIZE);
    let ty = Math.floor(Math.random() * GRID_SIZE);
    let tile = myTiles[ty][tx];
    let isOccupied =
        myUnits.some((u) => u.gridX === tx && u.gridY === ty) ||
        myItems.some((i) => i.gridX === tx && i.gridY === ty);
    if (tile.type !== "WATER" && !isOccupied) {
        let r = Math.random();
        let type;
        if (r < 0.2) type = "HEALTH_PACK";
        else if (r < 0.3) type = "BOMB1";
        else if (r < 0.4) type = "BOMB2";
        else if (r < 0.5) type = "BOMB3";
        else if (r < 0.6) type = "BLOP";
        else if (r < 0.7) type = "WHITE_BLOP";
        else if (r < 0.8) type = "EXPLOSION1";
        else if (r < 0.9) type = "EXPLOSION2";
        else type = "EXPLOSION3";
        myItems.push(new Item(tx, ty, type));
        console.log(`Spawned ${type} at ${tx}, ${ty}`);
    } else {
        spawnRandomItem();
    }
}

// Klassen
function component(width, height, img, x, y) {
    this.width = width;
    this.height = height;
    this.x = x;
    this.y = y;
    this.image = img;
    this.explored = false;
    this.type = "PLAINS";
    this.hasTrap = false;

    this.update = function () {
        let ctx = myGameArea.context;
        if (!this.explored) {
            ctx.fillStyle = "black";
            ctx.fillRect(this.x, this.y, this.width, this.height);
            return;
        }
        ctx.drawImage(this.image, this.x, this.y, this.width, this.height);
        if (this.hasTrap && assets.tiles.TRAP) {
            ctx.drawImage(assets.tiles.TRAP, this.x, this.y, this.width, this.height);
        }
        let isVisible = myUnits.some(
            (u) =>
                Math.abs(u.gridX - this.x / TILE_SIZE) <= 1 &&
                Math.abs(u.gridY - this.y / TILE_SIZE) <= 1
        );
        if (!isVisible) {
            ctx.fillStyle = "rgba(0, 0, 0, 0.3)";
            ctx.fillRect(this.x, this.y, this.width, this.height);
        }
    };
}

// Unit-Klasse um id und playerId erweitert
function unit(width, height, img, gridX, gridY, nameKey, id = 0, playerId = 0) {
    this.width = width;
    this.height = height;
    this.image = img;
    this.nameKey = nameKey || "UNIT";
    this.hp = 100;
    this.maxHp = 100;
    this.gridX = gridX;
    this.gridY = gridY;
    this.x = gridX * TILE_SIZE;
    this.y = gridY * TILE_SIZE;
    this.activeAction = null;
    this.id = id;
    this.playerId = playerId;

    this.update = function () {
        let ctx = myGameArea.context;
        let targetX = this.gridX * TILE_SIZE;
        let targetY = this.gridY * TILE_SIZE;
        this.x += (targetX - this.x) * 0.1;
        this.y += (targetY - this.y) * 0.1;

        if (selectedUnit === this) {
            ctx.strokeStyle = "white";
            ctx.lineWidth = 2;
            ctx.strokeRect(this.x, this.y, TILE_SIZE, TILE_SIZE);
        }
        ctx.drawImage(this.image, this.x + 5, this.y + 5, TILE_SIZE - 10, TILE_SIZE - 10);
        let barWidth = TILE_SIZE - 10;
        let barHeight = 5;
        let barX = this.x + 5;
        let barY = this.y - 8;
        ctx.fillStyle = "red";
        ctx.fillRect(barX, barY, barWidth, barHeight);
        ctx.fillStyle = "#00FF00";
        ctx.fillRect(barX, barY, barWidth * (this.hp / this.maxHp), barHeight);
    };

    this.moveTo = function (tx, ty) {
        this.gridX = tx;
        this.gridY = ty;
    };
}

function Item(gridX, gridY, type) {
    this.gridX = gridX;
    this.gridY = gridY;
    this.type = type;
    this.image = assets.tiles[type];
    this.x = gridX * TILE_SIZE;
    this.y = gridY * TILE_SIZE;
    this.bobOffset = Math.random() * Math.PI * 2;

    this.update = function () {
        let ctx = myGameArea.context;
        if (myTiles[this.gridY][this.gridX].explored) {
            let itemSize = TILE_SIZE * 0.6;
            let centerOffset = (TILE_SIZE - itemSize) / 2;
            let bob = Math.sin(Date.now() / 300 + this.bobOffset) * 3;
            ctx.drawImage(this.image, this.x + centerOffset, this.y + centerOffset + bob, itemSize, itemSize);
            ctx.fillStyle = "rgba(0,0,0,0.2)";
            ctx.beginPath();
            ctx.ellipse(this.x + TILE_SIZE / 2, this.y + TILE_SIZE - 5, itemSize / 3, 3, 0, 0, Math.PI * 2);
            ctx.fill();
        }
    };
}

let myParticles = [];
function Particle(x, y, color) {
    this.x = x;
    this.y = y;
    this.vx = (Math.random() - 0.5) * 4;
    this.vy = (Math.random() - 0.5) * 4 - 2;
    this.alpha = 1;
    this.color = color;
    this.update = function () {
        this.x += this.vx;
        this.y += this.vy;
        this.alpha -= 0.02;
    };
    this.draw = function (ctx) {
        ctx.globalAlpha = this.alpha;
        ctx.fillStyle = this.color;
        ctx.beginPath();
        ctx.arc(this.x, this.y, 3, 0, Math.PI * 2);
        ctx.fill();
        ctx.globalAlpha = 1.0;
    };
}

let myFloatingTexts = [];
function FloatingText(x, y, text, color) {
    this.x = x;
    this.y = y;
    this.text = text;
    this.color = color;
    this.alpha = 1;
    this.speed = 1.5;
    this.update = function () {
        this.y -= this.speed;
        this.alpha -= 0.015;
    };
    this.draw = function (ctx) {
        ctx.globalAlpha = this.alpha;
        ctx.fillStyle = this.color;
        ctx.font = "bold 18px Arial";
        ctx.textAlign = "center";
        ctx.shadowColor = "rgba(0,0,0,0.5)";
        ctx.shadowBlur = 4;
        ctx.fillText(this.text, this.x, this.y);
        ctx.shadowBlur = 0;
        ctx.globalAlpha = 1.0;
    };
}

// Maus- und Tastatur-Listener (Original)
canvas.addEventListener("mousemove", function (event) {
    let rect = canvas.getBoundingClientRect();
    let tX = Math.floor((event.clientX - rect.left) / TILE_SIZE);
    let tY = Math.floor((event.clientY - rect.top) / TILE_SIZE);
    if (tX >= 0 && tX < GRID_SIZE && tY >= 0 && tY < GRID_SIZE) {
        hoverTileX = tX;
        hoverTileY = tY;
    } else {
        hoverTileX = null;
        hoverTileY = null;
    }
    updateInfoPanel();
});

canvas.addEventListener("mouseleave", function () {
    hoverTileX = null;
    hoverTileY = null;
    updateInfoPanel();
});

window.addEventListener("keyup", function (e) {
    if (!selectedUnit || isMultiplayer) return; // Im Multiplayer keine lokale Bewegung
    let nextX = selectedUnit.gridX;
    let nextY = selectedUnit.gridY;
    switch (e.key.toLowerCase()) {
        case "w": nextY--; break;
        case "s": nextY++; break;
        case "a": nextX--; break;
        case "d": nextX++; break;
        default: return;
    }
    e.preventDefault();
    if (nextY >= 0 && nextY < GRID_SIZE && nextX >= 0 && nextX < GRID_SIZE) {
        let targetTile = myTiles[nextY][nextX];
        let action = ACTIONS.move;
        if (action.canExecute(selectedUnit, targetTile, nextX, nextY)) {
            action.execute(selectedUnit, targetTile, nextX, nextY);
            updateInfoPanel();
        }
    }
});

// ============================================
// DIALOG-FUNKTIONALITÄT (Single/Multiple Choice, etc.)
// ============================================
$(document).ready(function () {
    console.log("Initialisiere Dialoge...");

    // Prüfen, ob die Dialog-Elemente existieren
    var dialogs = [
        "#singleChoiceDialog",
        "#multipleChoiceDialog",
        "#dropdownDialog",
        "#dragDropDialog",
        "#freeTextDialog",
        "#rateQuestionDialog"
    ];
    dialogs.forEach(function (id) {
        if ($(id).length === 0) {
            console.error("Dialog-Element nicht gefunden: " + id);
        } else {
            console.log("Dialog gefunden: " + id);
        }
    });

    // Dialoge initialisieren (nur wenn vorhanden)
    try {
        $("#singleChoiceDialog").dialog({ autoOpen: false, modal: true, width: 400 });
        $("#multipleChoiceDialog").dialog({ autoOpen: false, modal: true, width: 400 });
        $("#dropdownDialog").dialog({ autoOpen: false, modal: true, width: 400 });
        $("#dragDropDialog").dialog({ autoOpen: false, modal: true, width: 400 });
        $("#freeTextDialog").dialog({ autoOpen: false, modal: true, width: 500 });
        $("#rateQuestionDialog").dialog({ autoOpen: false, modal: true, width: 500 });
        console.log("Dialoge initialisiert.");
    } catch (e) {
        console.error("Fehler-Details:", e.message)
    }

    // Single Choice
    $("#ui_btn_singleChoice").on("click", function () {
        console.log("Single Choice Button geklickt");

        loadQuestion('/fragen/Single-Choice.txt');

        $("#singleChoiceDialog").dialog("open");
    });
    $("#singleChoiceDialog button").on("click", function () {
        const answerText = $(this).text();
        alert("You selected: " + answerText);
        $("#singleChoiceDialog").dialog("close");
    });

    // Multiple Choice
    $("#ui_btn_multipleChoice").on("click", function () {
        console.log("Multiple Choice Button geklickt");

        loadQuestion('/fragen/Multiple-Choice.txt');

        $("#multipleChoiceDialog").dialog("open");
    });
    $("#submitMultipleChoice").on("click", function () {
        const selectedAnswers = [];
        $("#multipleChoiceDialog input[name='answer']:checked").each(function () {
            selectedAnswers.push($(this).parent().text().trim());
        });
        alert("You selected: " + selectedAnswers.join(", "));
        $("#multipleChoiceDialog").dialog("close");
    });

    // Dropdown
    $("#ui_btn_dropDown").on("click", function () {
        console.log("Dropdown Button geklickt");

        loadQuestion('/fragen/Dropdown.txt');

        $("#dropdownDialog").dialog("open");
    });
    $("#submitDropdown").on("click", function () {
        const selectedOption = $("#dropdownSelect").val();
        alert("You selected: " + selectedOption);
        $("#dropdownDialog").dialog("close");
    });

    // Drag & Drop
    $("#ui_btn_dragDrop").on("click", function () {
        console.log("Drag & Drop Button geklickt");

        loadQuestion('/fragen/Drag&Drop.txt');

        $("#dragDropDialog").dialog("open");
    });
    $(".draggable").draggable({ revert: "invalid", cursor: "move" });
    $(".droppable").droppable({
        accept: ".draggable",
        classes: { "ui-droppable-hover": "ui-droppable-hover" },
        drop: function (event, ui) {
            $(this).addClass("ui-state-highlight").text(ui.draggable.text());
            ui.draggable.hide();
            if ($(".draggable:visible").length === 0) {
                setTimeout(function () {
                    alert("Satz vervollständigt!");
                    $("#dragDropDialog").dialog("close");
                }, 500);
            }
        }
    });

    // Free Text
    $("#ui_btn_freeText").on("click", function () {
        console.log("Free Text Button geklickt");

        loadQuestion('/fragen/Freitext.txt');

        $("#freeTextDialog").dialog("open");
    });
    $("#submitFreeText").on("click", function () {
        const freeText = $("#freeTextInput").val();
        alert("You entered: " + freeText);
        $("#freeTextDialog").dialog("close");
    });

    // Rate Question
    $("#ui_btn_rateQuestion").on("click", function () {
        console.log("Rate Question Button geklickt");
        $("#rateQuestionDialog").dialog("open");
    });
    $("#rateQuestionDialog button").on("click", function () {
        const buttonText = $(this).text();
        alert("You clicked: " + buttonText);
        $("#rateQuestionDialog").dialog("close");
    });
});

const container = document.getElementById('quiz-container');
const singleChoiceDialog = document.getElementById('singleChoiceDialog');
const multipleChoiceDialog = document.getElementById('multipleChoiceDialog');
const dropdownDialog = document.getElementById('dropdownDialog');
const dragDropDialog = document.getElementById('dragDropDialog');
const freeTextDialog = document.getElementById('freeTextDialog');


function displaySingleChoice(data) {
    singleChoiceDialog.innerHTML = `
        <p><strong>${data.question}</strong></p>
        <form id="quizForm">
            ${data.options.map((opt, index) => `
                <div class="radio-option">
                    <input type="radio" id="choice-${index}" name="quiz-answer" value="${index}">
                    <label for="choice-${index}">${opt}</label>
                </div>
            `).join('')}
            <button type="button" onclick="checkRadioAnswer(${data.correctAnswer})">Submit</button>
        </form>
    `;
}

function displayMultipleChoice(data) {
    multipleChoiceDialog.innerHTML = `
        <p><strong>${data.question}</strong></p>
        <div class="checkbox-group">
            ${data.options.map((opt, index) => `
                <div class="option-row">
                    <input type="checkbox" id="opt-${index}" name="quiz-option" value="${index}">
                    <label for="opt-${index}">${opt}</label>
                </div>
            `).join('')}
        </div>
        <button onclick="checkMultipleAnswers()">Submit Answers</button>
    `;
}

function displayDragDrop(data) {
    // 1. Process the sentence
    // We use a global replace to find any placeholder format like ___1___
    let formattedSentence = data.sentence;

    data.placeholders.forEach(placeholder => {
        formattedSentence = formattedSentence.replace(
            placeholder,
            `<span class="droppable" data-placeholder="${placeholder}"></span>`
        );
    });

    // 2. Build the full HTML
    dragDropDialog.innerHTML = `
        <p class="drop-sentence">
            ${formattedSentence}
        </p>
        <div class="card-container">
            ${data.options.map(opt =>
        `<span class="draggable" draggable="true">${opt}</span>`
    ).join('')}
        </div>
    `;
}


function displayDropdown(data) {
    dropdownDialog.innerHTML = `
        <p>${data.question}</p>
        <select id="quizSelect">
            <option value="" disabled selected>Choose an option...</option>
            ${data.options.map((opt, index) =>
        `<option value="${index}">${opt}</option>`
    ).join('')}
        </select>
    `;
}

function displayFreeText(data) {
    freeTextDialog.innerHTML = `
      <p>${data.question}</p>
      <textarea id="freeTextInput" rows="4" cols="50"></textarea><br>
      <button id="submitFreeText" class="btn-primary">Submit</button>`;
}


function loadQuestion(url) {
    // Replace 'data.json' with the actual path to your file
    fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            // Now 'data' is your JavaScript object
            switch (data.type) {
                case "singleChoice": displaySingleChoice(data); break;
                case "multipleChoice": displayMultipleChoice(data); break;
                case "dragDrop": displayDragDrop(data); break;
                case "dropdown": displayDropdown(data); break;
                case "freeText": displayFreeText(data); break;
            }

        })
        .catch(error => {
            console.error('There was a problem with the fetch operation:', error);
        });
}

// ============================================
// NEUE WEBSOCKET-FUNKTIONALITÄT
// ============================================
let socket = null;
let isMultiplayer = false;
let myPlayerId = null;

function connectWebSocket() {
    const protocol = window.location.protocol === "https:" ? "wss:" : "ws:";
    const wsUrl = `${protocol}//${window.location.host}/ws`;
    socket = new WebSocket(wsUrl);

    socket.onopen = function () {
        console.log("WebSocket verbunden");
    };

    socket.onmessage = function (event) {
        const data = JSON.parse(event.data);
        handleServerMessage(data);
    };

    socket.onclose = function () {
        console.log("WebSocket geschlossen");
        isMultiplayer = false;
        myPlayerId = null;
    };
}

function handleServerMessage(msg) {
    switch (msg.type) {
        case "lobbyCreated":
            console.log("Lobby erstellt mit Code:", msg.lobbyCode);
            $("#lobbyCodeDisplay").text(msg.lobbyCode);
            updatePlayerList(msg.players);
            isMultiplayer = true;
            myPlayerId = msg.yourPlayerId;
            break;
        case "playerJoined":
            console.log("Spieler beigetreten:", msg.players);
            updatePlayerList(msg.players);
            break;
        case "gameStarted":
            console.log("Spiel gestartet!");
            updateLocalGameModel(msg.gameModel);
            break;
        case "gameStateUpdated":
            console.log("Spielzustand aktualisiert");
            updateLocalGameModel(msg.gameModel);
            break;
        case "error":
            console.error("Server-Fehler:", msg.message);
            alert("Fehler: " + msg.message);
            break;
        default:
            console.warn("Unbekannte Nachricht:", msg);
    }
}

function updatePlayerList(players) {
    const playerList = $("#playerList");
    playerList.empty();
    players.forEach(p => {
        playerList.append(`<p>${p.name} (ID: ${p.playerId})</p>`);
    });
}

function updateLocalGameModel(serverModel) {
    // Tiles aktualisieren
    for (let y = 0; y < GRID_SIZE; y++) {
        for (let x = 0; x < GRID_SIZE; x++) {
            const tile = myTiles[y][x];
            const serverTile = serverModel.tiles[y][x];
            tile.type = serverTile.type;
            tile.explored = serverTile.explored;
            tile.hasTrap = serverTile.hasTrap;
        }
    }

    // Units neu erstellen (mit id und playerId)
    myUnits = serverModel.units.map(u => {
        const unitObj = new unit(
            TILE_SIZE,
            TILE_SIZE,
            assets.tiles[`PIONEER_${u.nameKey}`],
            u.gridX,
            u.gridY,
            u.nameKey,
            u.id,
            u.playerId
        );
        unitObj.hp = u.hp;
        unitObj.maxHp = u.maxHp;
        return unitObj;
    });

    // Items
    myItems = serverModel.items.map(i => new Item(i.gridX, i.gridY, i.type));

    // Buildings (falls vorhanden)
    if (serverModel.buildings) {
        myBuildings = serverModel.buildings.map(b => {
            // Hier müsste ein Building-Konstruktor existieren – falls nicht, später ergänzen
            return null;
        }).filter(b => b !== null);
    }
}

// Lobby-UI
$("#ui_btn_lobby").click(function () {
    $("#lobbyDialog").dialog({
        modal: true,
        width: 400,
        open: function () {
            if (!socket || socket.readyState !== WebSocket.OPEN) {
                connectWebSocket();
            }
        }
    });
});

$("#btnCreateLobby").click(function () {
    const playerName = prompt("Dein Name?", "Spieler 1");
    if (playerName && socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({
            type: "createLobby",
            playerName: playerName
        }));
    } else {
        alert("Keine WebSocket-Verbindung.");
    }
});

$("#btnJoinLobby").click(function () {
    const code = $("#lobbyCodeInput").val();
    const playerName = prompt("Dein Name?", "Spieler 2");
    if (code && playerName && socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({
            type: "joinLobby",
            lobbyCode: code,
            playerName: playerName
        }));
    } else {
        alert("Keine WebSocket-Verbindung oder fehlender Code.");
    }
});

$("#btnStartGame").click(function () {
    const lobbyCode = $("#lobbyCodeDisplay").text();
    if (lobbyCode && socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({
            type: "startGame",
            lobbyCode: lobbyCode
        }));
    }
});

// ============================================
// ANGEPASSTE CANVAS-EVENT-HANDLER (ersetzen die ursprünglichen)
// ============================================
canvas.addEventListener("click", function (event) {
    menu.style.display = "none";

    let rect = canvas.getBoundingClientRect();
    let tX = Math.floor((event.clientX - rect.left) / TILE_SIZE);
    let tY = Math.floor((event.clientY - rect.top) / TILE_SIZE);

    let unitHit = myUnits.find((u) => u.gridX === tX && u.gridY === tY);
    let targetTile = myTiles[tY] ? myTiles[tY][tX] : null;

    if (selectedUnit && selectedUnit.activeAction) {
        if (isMultiplayer && socket && socket.readyState === WebSocket.OPEN) {
            socket.send(JSON.stringify({
                type: "playerAction",
                unitId: selectedUnit.id,
                action: selectedUnit.activeAction,
                targetX: tX,
                targetY: tY
            }));
            return;
        } else {
            const action = ACTIONS[selectedUnit.activeAction];
            if (action && action.canExecute(selectedUnit, targetTile, tX, tY)) {
                action.execute(selectedUnit, targetTile, tX, tY);
                updateInfoPanel();
            }
        }
        return;
    }

    if (!isMultiplayer) {
        if (unitHit) {
            selectedUnit = unitHit;
            if (!selectedUnit.activeAction) selectedUnit.activeAction = "move";
            updateInfoPanel();
            return;
        }
    } else {
        if (unitHit && unitHit.playerId === myPlayerId) {
            selectedUnit = unitHit;
            if (!selectedUnit.activeAction) selectedUnit.activeAction = "move";
            updateInfoPanel();
            return;
        }
    }
    updateInfoPanel();
});

canvas.addEventListener("contextmenu", function (e) {
    e.preventDefault();

    let rect = canvas.getBoundingClientRect();
    let tX = Math.floor((e.clientX - rect.left) / TILE_SIZE);
    let tY = Math.floor((e.clientY - rect.top) / TILE_SIZE);

    if (selectedUnit &&
        selectedUnit.gridX === tX &&
        selectedUnit.gridY === tY &&
        (!isMultiplayer || selectedUnit.playerId === myPlayerId)) {
        menu.style.display = "block";
        menu.style.left = e.pageX + "px";
        menu.style.top = e.pageY + "px";
    } else {
        menu.style.display = "none";
    }
});

// ============================================
// INITIALISIERUNG (angepasst)
// ============================================
function onStartGame() {
    rebuildContextMenu();

    assets.loadAll(() => {
        myGameArea.start();

        for (let y = 0; y < GRID_SIZE; y++) {
            myTiles[y] = [];
            for (let x = 0; x < GRID_SIZE; x++) {
                let rand = Math.random();
                let terrainKey = "PLAINS";
                if (rand < 0.1) terrainKey = "MOUNTAIN";
                else if (rand < 0.2) terrainKey = "WATER";
                else if (rand < 0.4) terrainKey = "FOREST";

                let tile = new component(
                    TILE_SIZE,
                    TILE_SIZE,
                    assets.tiles[terrainKey],
                    x * TILE_SIZE,
                    y * TILE_SIZE
                );
                tile.type = terrainKey;
                myTiles[y][x] = tile;
            }
        }

        if (!isMultiplayer) {
            myUnits.push(new unit(TILE_SIZE, TILE_SIZE, assets.tiles.PIONEER_RED, 1, 1, "RED", 0, 0));
            myUnits.push(new unit(TILE_SIZE, TILE_SIZE, assets.tiles.PIONEER_BLUE, 1, 28, "BLUE", 1, 1));
            myUnits.push(new unit(TILE_SIZE, TILE_SIZE, assets.tiles.PIONEER_YELLOW, 28, 1, "YELLOW", 2, 2));
            myUnits.push(new unit(TILE_SIZE, TILE_SIZE, assets.tiles.PIONEER_GREEN, 28, 28, "GREEN", 3, 3));
        }

        setInterval(spawnRandomItem, 3000);
    });

    document.getElementById("ui_btn_build").addEventListener("click", () => setAction("placeTrap"));
    document.getElementById("ui_btn_test").addEventListener("click", () => setAction("test"));
    document.getElementById("ui_btn_move").addEventListener("click", () => setAction("move"));
    document.getElementById("ui_btn_fight").addEventListener("click", () => setAction("attack"));
}




// Start
$(document).ready(function () {
    onStartGame();
});

