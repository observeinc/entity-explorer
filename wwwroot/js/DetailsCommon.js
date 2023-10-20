$(document).ready(function() {
    hljs.highlightAll();
});

function goToGraphVizOnline(textArea)
{
    var graphVizCode = $("#" + textArea).text();
    
    var graphVizUrlRoot = "https://edotor.net";

    var graphVizUrl = graphVizUrlRoot + "/?engine=dot#" + encodeURI(graphVizCode);
    console.log(graphVizUrl);

    window.open(graphVizUrl, "_blank");
}