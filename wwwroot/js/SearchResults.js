$(document).ready(function() {
    hljs.highlightAll();
    hljs.initLineNumbersOnLoad();

    var keyword = $("#searchQuery").text();

    // Highlight as text
    var optionsMark = {
        "element": "mark",
        "className": "",
        "exclude": [],
        "separateWordSearch": true,
        "accuracy": "partially",
        "diacritics": true,
        "synonyms": {},
        "iframes": false,
        "iframesTimeout": 5000,
        "acrossElements": false,
        "caseSensitive": false,
        "ignoreJoiners": false,
        "ignorePunctuation": [],
        "wildcards": "disabled",
        "each": function(node){
            // node is the marked DOM element
        },
        "filter": function(textNode, foundTerm, totalCounter, counter){
            // textNode is the text node which contains the found term
            // foundTerm is the found search term
            // totalCounter is a counter indicating the total number of all marks
            //              at the time of the function call
            // counter is a counter indicating the number of marks for the found term
            return true; // must return either true or false
        },
        "noMatch": function(term){
            // term is the not found term
        },
        "done": function(counter){
            // counter is a counter indicating the total number of all marks
        },
        "debug": false,
        "log": window.console
    }
    $("code.language-javascript").mark(keyword, optionsMark);

    // Highlight as regex
    var optionsMarkRegExp = {
        "element": "mark",
        "className": "",
        "exclude": [],
        "iframes": false,
        "iframesTimeout": 5000,
        "acrossElements": false,
        "ignoreGroups": 0,
        "each": function(node){
            // node is the marked DOM element
        },
        "filter": function(textNode, foundTerm, totalCounter){
            // textNode is the text node which contains the found term
            // foundTerm is the found search term
            // totalCounter is a counter indicating the total number of all marks
            //              at the time of the function call
            return true; // must return either true or false
        },
        "noMatch": function(term){
            // term is the not found term
        },
        "done": function(counter){
            // counter is a counter indicating the total number of all marks
        },
        "debug": false,
        "log": window.console
    };
    //var flags = keyword.replace(/.*\/([gimy]*)$/, '$1');
    var regex = new RegExp(keyword, "gmi");
    $("code.language-javascript").markRegExp(regex, optionsMarkRegExp);
});

function showOrHideOPALPreviews(checkBox)
{
    if (checkBox.checked == true)
    {
        $("pre[id^='code_']").show();
        $("input[id^='checkboxOPALPreview_']").prop("checked", true)
    }
    else
    {
        $("pre[id^='code_']").hide();
        $("input[id^='checkboxOPALPreview_']").prop("checked", false)
    }
}

function showOrHideOPALPreview(checkBox, preBlockID)
{
    if (checkBox.checked == true)
    {
        $("pre[id='" +preBlockID + "']").show();
    }
    else
    {
        $("pre[id='" +preBlockID + "']").hide();
    }
}