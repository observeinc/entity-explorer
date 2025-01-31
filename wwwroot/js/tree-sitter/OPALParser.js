var tree;

var parser = null;
var opalParser = null;

$(document).ready(function() {
  // hljs.highlightAll();
  // hljs.initLineNumbersOnLoad();

  (async () => {
    await TreeSitter.init();    
    parser = new TreeSitter();
    try {
      opalParser = await TreeSitter.Language.load("/js/tree-sitter/tree-sitter-opal.wasm");
    } catch (e) {
      console.error(e);
      return
    } finally {
    }    
    parser.setLanguage(opalParser);
  })();

});

function parseOPAL(codeAreaID)
{
  var OPALCode = $("#" + codeAreaID).text();

  var newTree = parser.parse(OPALCode, tree);
  if (tree) tree.delete();
  tree = newTree; 

  const cursor = tree.walk();
  let row = '';
  let rows = [];
  let visitedChildren = false;
  let finishedRow = false;
  let indentLevel = 0;

  for (let i = 0;; i++) 
  {
    let displayName;
    if (cursor.nodeIsMissing) {
      displayName = `MISSING ${cursor.nodeType}`
    } else if (cursor.nodeIsNamed) {
      displayName = cursor.nodeType;
    }

    if (visitedChildren) {
      if (displayName) {
        finishedRow = true;
      }

      if (cursor.gotoNextSibling()) {
        visitedChildren = false;
      } else if (cursor.gotoParent()) {
        visitedChildren = true;
        indentLevel--;
      } else {
        break;
      }
    } else {
      if (displayName) {
        if (finishedRow) {
          //row += '</div>';
          row += '</tr>';
          rows.push(row);
          finishedRow = false;
        }
        const start = cursor.startPosition;
        const end = cursor.endPosition;
        const id = cursor.nodeId;
        let fieldName = cursor.currentFieldName();
        let nodeText = cursor.nodeText.trim();
        switch (fieldName)
        {
          case "argument":
          case "arguments":
          case "expression":
          case "line_comment":
          case "left":
          case "member":
          case "name":
          case "operand":
          case "operator":
          case "right":
          case "statement":
          case "string-literal":
              break;
          
          default:
            nodeText = '';  
            break;
        }
        if (fieldName) {
          fieldName += ': ';
        } else {
          fieldName = '';
        }        
        //row = `<div>${'  '.repeat(indentLevel)}${fieldName} ${displayName} [${start.row}, ${start.column}] - [${end.row}, ${end.column}] ${nodeText}`;
        row = `<tr><td>${'  '.repeat(indentLevel)}${fieldName} ${displayName} [${start.row}, ${start.column}] - [${end.row}, ${end.column}]</td><td> ${nodeText}</td>`;
        finishedRow = true;
      }

      if (cursor.gotoFirstChild()) {
        visitedChildren = false;
        indentLevel++;
      } else {
        visitedChildren = true;
      }
    }
  }

  if (finishedRow) {
    //row += '</div>';
    row += '</tr>';
    rows.push(row);
  }

  cursor.delete();

  $("#code_parsed").html("<table border='1'>" + rows + "</table>");

}