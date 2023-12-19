$(document).ready( function() {
    //$('[id^="datasetTable"]').DataTable( {
    $(".SortableTable").DataTable( {
        paging: false
    });
    hljs.highlightAll();
    hljs.initLineNumbersOnLoad();

    
});

function goToGraphVizOnline(textArea)
{
    var graphVizCode = $("#" + textArea).text();
    
    var graphVizUrlRoot = "https://edotor.net";

    var graphVizUrl = graphVizUrlRoot + "/?engine=dot#" + encodeURI(graphVizCode);
    console.log(graphVizUrl);

    window.open(graphVizUrl, "_blank");
}