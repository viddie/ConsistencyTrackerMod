
const ViewStates = {
    MainView: 0,
    InspectorView: 1,
};
let CurrentState = null;

const Elements = {
    MainContainer: "main-view",
    LoadingText: "loading-formats-text",

    InspectorContainer: "format-view",
    FormatList: "format-list",
    CreateFormatButton: "create-format-button",

    FormatName: "format-name",
    FormatText: "format-text",
    FormatPreview: "format-preview",
    FormatSaveButton: "format-save-button",

    PlaceholderList: "placeholder-list",

    PlaceholderExplanationName: "placeholder-explanation-name",
    PlaceholderExplanationDescription: "placeholder-explanation-description",
};

//#region Properties

let customFormats = null;
let defaultFormats = null;
let placeholders = null;

//#endregion


//#region Startup
document.addEventListener("DOMContentLoaded", function () {
    loadElements(Elements);
    ShowState(ViewStates.MainView);
});

function ShowState(state) {
    CurrentState = state;
    switch (state) {
        case ViewStates.MainView:
            Elements.MainContainer.style.display = "flex";
            Elements.InspectorContainer.style.display = "none";
            OnShowMainView();
            break;
        case ViewStates.InspectorView:
            Elements.MainContainer.style.display = "none";
            Elements.InspectorContainer.style.display = "flex";
            OnShowInspectorView();
            break;
    }
}

function showError(errorCode, errorMessage){
    let message = "Error ("+errorCode+"): "+errorMessage;
    console.log(message);

    if(CurrentState === ViewStates.MainView){
        Elements.LoadingText.innerText = message;
    }else if(CurrentState === ViewStates.InspectorView){
        Elements.FormatPreview.value = message;
    }
}
//#endregion


//#region MainView
function OnShowMainView() {
    setTimeout(() => {
        fetchAvailableFormats(afterFetchRoomLayout);
    }, 700);
}

function fetchAvailableFormats(then){
    let url = "http://localhost:32270/cct/getFormatsList";
    fetch(url)
        .then(response => response.json())
        .then(responseObj => {
            console.log(responseObj);
            if(responseObj.errorCode !== 0){
                showError(responseObj.errorCode, responseObj.errorMessage);
                return;
            }

            customFormats = responseObj.customFormats;
            defaultFormats = responseObj.defaultFormats;

            if(CurrentState === ViewStates.InspectorView){
                changedFormatName();
            }

            then();
        }).catch(error => showError(-1, "Could not fetch formats (is CCT running? is debug mode enabled?)"));
}
function afterFetchRoomLayout(){
    fetchPhysicsLog(goToInspectorView);
}

function fetchPhysicsLog(then){
    let url = "http://localhost:32270/cct/getPlaceholderList";
    fetch(url)
        .then(response => response.json())
        .then(responseObj => {
            console.log(responseObj);
            if(responseObj.errorCode !== 0){
                showError(responseObj.errorCode, responseObj.errorMessage);
                return;
            }

            placeholders = responseObj.placeholders;
            then();
        })
        .catch(error => showError(-1, "Could not fetch placeholders (is CCT running?)"));
}
function goToInspectorView(){
    ShowState(ViewStates.InspectorView);
}
//#endregion


//#region EditView
function OnShowInspectorView() {
    //LEFT SIDEBAR
    createFormatList();

    //add click listener to create format button
    Elements.CreateFormatButton.addEventListener("click", function(){
        Elements.FormatName.value = "";
        Elements.FormatText.value = "Type your format here...\nDon't forget to give it a name!";
        changedFormatName();
    });

    //RIGHT SIDEBAR
    createPlaceholderList();
    setPlaceholderExplanationText("", "");
    //periodically update the preview
    setInterval(fetchPreview, 1000);

    //MAIN CONTAINER
    //add on change listener to format name
    Elements.FormatName.addEventListener("input", function(){
        changedFormatName();
    });

    //add on change listener to format text
    Elements.FormatText.addEventListener("input", function(){
        changedFormatText();
    });

    //add on click listener to save button
    Elements.FormatSaveButton.addEventListener("click", function(){
        saveFormat();
    });
}

function changedFormatName(){
    let formatName = Elements.FormatName.value;
    //If the format name exists in the default or custom formats, set the save button text to "Save as 'name'"
    let formatExists = false;
    for (let i = 0; i < defaultFormats.length; i++) {
        let format = defaultFormats[i];
        if(format.name === formatName){
            formatExists = true;
            break;
        }
    }
    if(!formatExists){
        for (let i = 0; i < customFormats.length; i++) {
            let format = customFormats[i];
            if(format.name === formatName){
                formatExists = true;
                break;
            }
        }
    }

    if(formatExists){
        Elements.FormatSaveButton.innerText = "Save as '"+formatName+"'";
    }else{
        Elements.FormatSaveButton.innerText = "Create new Format '"+formatName+"'";
    }

    let hasDisabled = false;
    //If the format text is empty, set the save button text to "Delete Format"
    if(Elements.FormatText.value === ""){
        //if the format name doesnt exist, set the save button text to "Delete Format"
        if(!formatExists){
            Elements.FormatSaveButton.setAttribute("disabled", "");
            Elements.FormatSaveButton.innerText = "Cannot delete format that doesnt exist";
            hasDisabled = true;
        }else{
            Elements.FormatSaveButton.innerText = "Delete Format '"+formatName+"'";
        }
    }

    //If the format name is empty, disable the save button
    if(formatName === ""){
        Elements.FormatSaveButton.setAttribute("disabled", "");
        Elements.FormatSaveButton.innerText = "Name cannot be empty";
    }else{
        if(!hasDisabled){
            Elements.FormatSaveButton.removeAttribute("disabled");
        }
    }
}

function changedFormatText(){
    //when text is empty, set the save button text to "Delete Format"
    if(Elements.FormatText.value === "" && Elements.FormatName.value !== ""){
        Elements.FormatSaveButton.innerText = "Delete Format '"+Elements.FormatName.value+"'";
    }else{
        changedFormatName();
    }
}

function saveFormat(){
    //check if save button is disabled
    if(Elements.FormatSaveButton.hasAttribute("disabled")){
        return;
    }

    let formatName = Elements.FormatName.value;
    let formatText = Elements.FormatText.value;

    let url = "http://localhost:32270/cct/saveFormat";
    let data = {
        name: formatName,
        format: formatText
    };
    console.log("Sending request to save format: "+formatName+" with format: "+formatText+" to url "+url+"")
    fetch(url, {
        method: "POST",
        headers: {
            "Accept": "application/json",
        },
        body: JSON.stringify(data)
    }).then(response => response.json())
        .then(responseObj => {
            console.log(responseObj);
            if(responseObj.errorCode !== 0){
                showError(responseObj.errorCode, responseObj.errorMessage);
                return;
            }

            //if the format was deleted, clear the format name and format text
            if(formatText === ""){
                Elements.FormatName.value = "";
                Elements.FormatText.value = "";
                changedFormatName();
            }

            //update the format list
            fetchAvailableFormats(createFormatList);
        }).catch(error => showError(-1, "Could not save format (is CCT running?)"));
}

//#region Format List
function createFormatList(){
    Elements.FormatList.innerHTML = "";

    //Add h5 element with text "Custom Formats"
    let h5 = document.createElement("h5");
    h5.innerText = "Custom Formats";
    Elements.FormatList.appendChild(h5);

    //Add hr element inbetween
    let hr = document.createElement("hr");
    Elements.FormatList.appendChild(hr);

    for (let i = 0; i < customFormats.length; i++) {
        let format = customFormats[i];
        let element = createFormatElement(format);
        Elements.FormatList.appendChild(element);
    }

    //Add another hr element
    Elements.FormatList.appendChild(hr.cloneNode());

    //Add another h5 element with text "Default Formats"
    let h52 = document.createElement("h5");
    h52.innerText = "Default Formats";
    Elements.FormatList.appendChild(h52);
    

    //Add another hr element
    Elements.FormatList.appendChild(hr.cloneNode());

    for (let i = 0; i < defaultFormats.length; i++) {
        let format = defaultFormats[i];
        let element = createFormatElement(format);
        Elements.FormatList.appendChild(element);
    }
}
//creates an element like <a class="button">full-golden-attempts</a>
function createFormatElement(formatObj){
    let element = document.createElement("a");
    element.classList.add("button");
    element.innerText = formatObj.name;
    element.onclick = function(){
        clickedFormat(formatObj);
    };
    return element;
}
function clickedFormat(formatObj){
    console.log("clicked format: " + formatObj.name + ", format: " + formatObj.format);
    Elements.FormatName.value = formatObj.name;
    Elements.FormatText.value = formatObj.format;
    changedFormatName();
}
//#endregion

//#region Placeholder List
function createPlaceholderList(){
    //Placeholder format: {groupName:placeholderName}

    //Clear the placeholder list
    Elements.PlaceholderList.innerHTML = "";

    //Create an object with all the groups
    let groups = {};

    //sort the placeholders into the groups
    for (let i = 0; i < placeholders.length; i++) {
        let placeholder = placeholders[i];
        let groupName = placeholder.name.substring(1, placeholder.name.indexOf(":"));
        if(groups[groupName] === undefined){
            groups[groupName] = [];
        }
        groups[groupName].push(placeholder);
    }

    //Sort the placeholders in the groups alphabetically
    for (const groupName in groups) {
        if (!groups.hasOwnProperty(groupName)) {
            continue;
        }
        const group = groups[groupName];
        group.sort(function(a, b){
            if (a.name < b.name) {
                return -1;
            }
            if (a.name > b.name) {
                return 1;
            }
        });
    }

    //Sort the groups alphabetically
    let groupNames = [];
    for (const groupName in groups) {
        if (!groups.hasOwnProperty(groupName)) {
            continue;
        }
        groupNames.push(groupName);
    }
    groupNames.sort();
    console.log(groupNames);

    //For each group, create a header h5 element and a ul element
    for (i = 0; i < groupNames.length; i++) {
        const groupName = groupNames[i];
        const group = groups[groupName];

        //Create h5 element, capitalize first letter
        let h5 = document.createElement("h5");
        h5.innerText = groupName.charAt(0).toUpperCase() + groupName.slice(1);
        Elements.PlaceholderList.appendChild(h5);

        //Create ul element
        let ul = document.createElement("ul");
        for (let i = 0; i < group.length; i++) {
            let placeholder = group[i];
            let element = createPlaceholderElement(placeholder);
            ul.appendChild(element);
        }
        Elements.PlaceholderList.appendChild(ul);
    }
}

/*
Creates an element like
<li>{room:successRate}</li>
*/
function createPlaceholderElement(placeholderObj){
    let element = document.createElement("li");
    element.innerText = placeholderObj.name;
    //On hover, show explanation
    element.onmouseover = function(){
        setPlaceholderExplanationText(placeholderObj.name, placeholderObj.description);
    };
    //On mouse exit, hide explanation
    element.onmouseout = function(){
        setPlaceholderExplanationText("", "");
    };
    //On click, insert placeholder into format text textarea
    element.onclick = function(){
        let pos = Elements.FormatText.selectionStart;
        let val = Elements.FormatText.value;
        let start = val.substring(0, pos);
        let end = val.substring(pos, val.length);
        Elements.FormatText.value = start + placeholderObj.name + end;
        
        //And give focus back to the textarea
        Elements.FormatText.focus();

        //And set the textarea selection to the end of the inserted placeholder
        Elements.FormatText.selectionStart = pos + placeholderObj.name.length;
        Elements.FormatText.selectionEnd = pos + placeholderObj.name.length;
    };
    return element;
}

function setPlaceholderExplanationText(name, description){
    Elements.PlaceholderExplanationName.innerText = name;
    Elements.PlaceholderExplanationDescription.innerText = description;
}

//Launch POST request to /cct/parseFormat with the format text as body
function fetchPreview(){
    let formatText = Elements.FormatText.value;
    if(formatText === null || formatText === undefined || formatText === ""){
        Elements.FormatPreview.value = "";
        return;
    }

    let url = "http://localhost:32270/cct/parseFormat";
    fetch(url, {
        method: "POST",
        body: JSON.stringify({formats: [formatText]})
    }).then(response => response.json())
        .then(responseObj => {
            if(responseObj.errorCode !== 0){
                console.log(responseObj.errorMessage);
                Elements.FormatPreview.value = "Could not fetch preview, error code (" + responseObj.errorCode + "): " + responseObj.errorMessage;
                return;
            }

            Elements.FormatPreview.value = responseObj.formats[0];
        }).catch(error => showError(-1, "Could not fetch preview (is CCT running?)"));
}
//#endregion

//#endregion