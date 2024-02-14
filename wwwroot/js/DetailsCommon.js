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

function showOrHideDashboardWidgetImagePreview(checkBox)
{
    if (checkBox.checked == true)
    {
        $("tr[id^='stage_image_preview_']").show();
    }
    else
    {
        $("tr[id^='stage_image_preview_']").hide();
    }
}

function showOrHideDashboardInputsOutputTables(checkBox)
{
    if (checkBox.checked == true)
    {
        $("tr[id^='stage_table_input_']").show();
        $("tr[id^='stage_table_output_']").show();
    }
    else
    {
        $("tr[id^='stage_table_input_']").hide();
        $("tr[id^='stage_table_output_']").hide();
    }
}