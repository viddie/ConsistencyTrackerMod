
const ViewStates = {
    MainView: 0,
    EditView: 1,
};

const Elements = {
    MainContainer: "main-view",

    FormatContainer: "format-view",
    FormatList: "format-list",

    FormatName: "format-name",
    FormatText: "format-text",
    FormatPreview: "format-preview",
    FormatSaveButton: "format-save-button",

    PlaceholderList: "placeholder-list",

    PlaceholderExplanationName: "placeholder-explanation-name",
    PlaceholderExplanationDescription: "placeholder-explanation-description",
};

//#region Properties

let formats = null;
let placeholders = null;

//#endregion


//#region Startup
document.addEventListener("DOMContentLoaded", function () {
    loadElements(Elements);
    ShowState(ViewStates.MainView);
});

function ShowState(state) {
    switch (state) {
        case ViewStates.MainView:
            Elements.MainContainer.style.display = "flex";
            Elements.FormatContainer.style.display = "none";
            OnShowMainView();
            break;
        case ViewStates.EditView:
            Elements.MainContainer.style.display = "none";
            Elements.FormatContainer.style.display = "flex";
            OnShowEditView();
            break;
    }
}
//#endregion


//#region MainView
function OnShowMainView() {
    fetchAvailableFormats();
}

function fetchAvailableFormats(){
    let url = "http://localhost:32270/cct/getFormatsList";
    fetch(url)
        .then(response => response.json())
        .then(responseObj => {
            console.log(responseObj);
            if(responseObj.errorCode !== 0){
                console.log(responseObj.errorMessage);
                return;
            }

            formats = responseObj.formats;
            fetchPlaceholders();
        }).catch(error => console.log(error));
}

function fetchPlaceholders(){
    let url = "http://localhost:32270/cct/getPlaceholderList";
    fetch(url)
        .then(response => response.json())
        .then(responseObj => {
            console.log(responseObj);
            if(responseObj.errorCode !== 0){
                console.log(responseObj.errorMessage);
                return;
            }

            placeholders = responseObj.placeholders;
            ShowState(ViewStates.EditView);
        })
        .catch(error => console.log(error));
}
//#endregion


//#region EditView
function OnShowEditView() {
    Elements.FormatList.innerHTML = "";
    for (let i = 0; i < formats.length; i++) {
        let format = formats[i];
        let element = createFormatElement(format);
        Elements.FormatList.appendChild(element);
    }

    //Create unordered list of placeholders based on the array "placeholders"
    Elements.PlaceholderList.innerHTML = "";
    let ul = document.createElement("ul");
    for (let i = 0; i < placeholders.length; i++) {
        let placeholder = placeholders[i];
        let element = createPlaceholderElement(placeholder);
        ul.appendChild(element);
    }
    Elements.PlaceholderList.appendChild(ul);

    //periodically update the preview
    setInterval(fetchPreview, 1000);
}

//Launch POST request to /cct/parseFormat with the format text as body
function fetchPreview(){
    let formatText = Elements.FormatText.value;
    if(formatText === null || formatText === undefined || formatText === ""){
        Elements.FormatPreview.innerText = "";
        return;
    }

    let url = "http://localhost:32270/cct/parseFormat";
    fetch(url, {
        method: "POST",
        body: JSON.stringify({formats: [formatText]})
    }).then(response => response.json())
        .then(responseObj => {
            console.log(responseObj);
            if(responseObj.errorCode !== 0){
                console.log(responseObj.errorMessage);
                return;
            }

            Elements.FormatPreview.value = responseObj.formats[0];
        }).catch(error => console.log(error));
}

//creates an element like <a class="button">full-golden-attempts</a>
function createFormatElement(formatObj){
    let element = document.createElement("a");
    element.classList.add("button");
    element.innerText = formatObj.Name;
    element.onclick = function(){
        clickedFormat(formatObj);
    };
    return element;
}

function clickedFormat(formatObj){
    console.log("clicked format: " + formatObj.Name + ", format: " + formatObj.Format);
    Elements.FormatName.value = formatObj.Name;
    Elements.FormatText.value = formatObj.Format;
}

/*
Creates an element like
<li>{room:successRate}</li>
*/
function createPlaceholderElement(placeholderObj){
    let element = document.createElement("li");
    element.innerText = placeholderObj.Key;
    //On hover, show explanation
    element.onmouseover = function(){
        Elements.PlaceholderExplanationName.innerText = placeholderObj.Key;
        Elements.PlaceholderExplanationDescription.innerText = placeholderObj.Value;
    };
    //On mouse exit, hide explanation
    element.onmouseout = function(){
        Elements.PlaceholderExplanationName.innerText = "";
        Elements.PlaceholderExplanationDescription.innerText = "";
    };
    return element;
}
//#endregion