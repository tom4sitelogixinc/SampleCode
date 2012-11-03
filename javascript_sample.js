/* Sample functions to handle jQuery plugins and
 * a jasonP call to a cross domain server
 */

$(function () {
    function openOurList(event) {
        GetFuneralHomes();
        $("#dialog-form").dialog("open");
        return false;
    }
    $('#clickdeliverid').click(openOurList);
});

function UpdateShipForm(storeName, storeAddress, listClickIndex) {
    var storeElements = storeAddress.split(",");
    var storeElements2 = storeElements[1].split("TX");
    document.getElementById('pname').value = storeName;
    document.getElementById('paddr1').value = storeElements[0];
    document.getElementById('paddr2').value = '';
    document.getElementById('pcity').value = storeElements2[0];
    document.getElementById('pstate').options[zgel('pstate').selectedIndex].value = "TX";
    document.getElementById('pzip').value = storeElements2[1];
    $("#dialog-form").dialog("close");
}

function sortFHList(arr) {
    // Setup Arrays
    var sortedKeys = new Array();
    var sortedObj = {};

    // Separate keys and sort them
    for (var i in arr) {
        sortedKeys.push(i);
    }
    sortedKeys.sort();

    // Reconstruct sorted obj based on keys
    for (var i in sortedKeys) {
        sortedObj[sortedKeys[i]] = arr[sortedKeys[i]];
    }
    return sortedObj;
}

$(function () {
    $("#dialog:ui-dialog").dialog("destroy");
    tips = $(".validateTips");
    function updateTips(t) {
        tips
				.text(t)
				.addClass("ui-state-highlight");
        setTimeout(function () {
            tips.removeClass("ui-state-highlight", 1500);
        }, 500);
    }

    $("#dialog-form").dialog({
        autoOpen: false,
        height: 500,
        width: 400,
        modal: true,
        buttons: {
            "Exit Without Selecting": function () {
                $(this).dialog("close");
            }
        },
        close: function () {
            
        }
    });
});

// Function to get the selected funeral home's location data from an xml file
function GetFuneralHomes() {
    $.get("locs.xml", {}, function (xml) {
        $(xml).find("locationinfo").each(function () {
            var loctype = $(this).find('loctype').text();
            if (loctype == 'fh') {
                var storeid = $(this).find('storeid').text();
                var storename = $(this).find('storename').text();
                var hours = $(this).find('hours').text();
                var address = $(this).find('address').text();
                var address2 = $(this).find('address2').text();
                var telephone = $(this).find('telephone').text();

                storenameescapeda = storename.replace("'", "&#39;");
                addressescapeda = address.replace("'", "&#39;");
                address2escapeda = address2.replace("'", "&#39;");

                var plusEsc = "+";
                var storenameescaped = storenameescapeda.replace(/ /g, plusEsc);
                var addressescaped = addressescapeda.replace(/ /g, plusEsc);
                var address2escaped = address2escapeda.replace(/ /g, plusEsc);

                var z = 0;
                var listHTMLtex = '';
                listHTMLtex += "<table class='listingContainer'><tr><td class='listingIcon' valign='middle' align='center' width='10px'>";
                listHTMLtex += "<\/td>";
                listHTMLtex += "<td class='fhSelectLink' width='320px'>";
                listHTMLtex += "<a class='fhSelectLink' href='javascript:UpdateShipForm(\"" + storename + "\",\"" + address + "\"," + z + ");'><b>";
                listHTMLtex += "&nbsp;&nbsp;" + storename + "</b><br/>";
                listHTMLtex += "&nbsp;&nbsp;" + address;
                listHTMLtex += "<br /><\/a>";
                listHTMLtex += "<\/td><\/tr><\/table>";
                var listSort = storename.toUpperCase();
                listHTML[listSort] = listHTMLtex;
            }
        });
        var nListHTML = sortFHList(listHTML);
        var cscrl = $('#scrollable');
        for (x in nListHTML) {
            cscrl.append(nListHTML[x]);
        }
    });
}

function Jsonp(url) {
    var script = document.createElement("script");
    script.setAttribute("src", url);
    script.setAttribute("type", "text/javascript");
    document.getElementsByTagName('head')[0].appendChild(script);
}

function JsonPCallBack(result) {
    var addataData = result.sliFHKnownBy1 + "|" + result.sliFHAddress1 + "|" + result.sliFHAddress2 + "|" + result.sliFHCity + "|" + result.sliFHState + "|" + result.sliFHZip + "|" + result.sliFHPhone + "|" + result.sliUrl;
    setCookie("addata", addataData, 1);
    setCookie("inmemory", result.sliObitFullName, 1);    
}

function GetFhData() {
    var aFhid = getQueryString()["fhid"];
    var aPid = getQueryString()["pid"];
    var aCobrand = getQueryString()["cobrand"];
    var url = "http://www.sitelogixinc.com/JasonpWebService?callback=JsonPCallBack&fhid=" + aFhid + "&cobrand=" + aCobrand + "&pid=" + aPid;
    Jsonp(url);
}

function GetFHRB1() {
    document.getElementById('RB1').checked = true;
    document.getElementById('RB2').checked = false;
    var val1 = document.getElementById('presidence').style.display;
    if (val1 === 'block') {
        document.getElementById('presidence').style.display = 'none';
    }
}

function GetFHRB2() {
    document.getElementById('RB1').checked = false;
    document.getElementById('RB2').checked = true;
    var val1 = document.getElementById('presidence').style.display;
    if (val1 === 'none') {
        document.getElementById('presidence').style.display = 'block';
    }
}                 