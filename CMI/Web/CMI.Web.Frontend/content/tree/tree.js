$(function() {
	$("a").on("click",function() {
		var l=$(this);
		if (l.attr("href").indexOf("void")>-1) {
			var n=l.siblings("ul");
			if (n.is(":visible")) {
				n.find("ul:visible").hide();
				n.find("a.treesel").removeClass("treesel");
				l.removeClass("treesel");
				n.hide();
			} else {
				//l.addClass("treesel");
				l.parents("li").find(">a").addClass("treesel");				
				var tableLinks=n.find("> li > a.treelink");
				if (tableLinks.length>0) {
					var table="<table class=\"tablesorter\"><thead><tr><th>"+i18n["sign"]+"</th><th>"+i18n["title"]+"</th><th>"+i18n["period"]+"</th></tr></thead><tbody>";
					tableLinks.each(function() {
						var cl=$(this);
						table+="<tr><td>"+cl.data("sign")+"</td><td><a class=\"treelink\" href=\""+cl.attr("href")+"\" target=\"_blank\">"+cl.text()+"</a></td><td>"+cl.data("period")+"</td></tr>";
					});
					table+="</tbody></table>";
					table=$(table);
					n.empty().append(table);
					table.tablesorter( {"sortList": [[1,0]] });
				}
				l.parents("ul").show();
				n.show();
			}
			return false;
		}
	});
	 $(window).hashchange(function() {
			directLink();
		});
	 directLink();
});      	

function directLink() {
	if(window.parent.location.hash && window.parent.location.hash.lastIndexOf('#') > 1) {
		var hash = window.parent.location.hash;
		var pos = hash.lastIndexOf('#')
		var elementId = hash.substring(pos+1);
		$("#tree > li > a.treesel").trigger("click");
		$("#"+elementId).trigger("click");  	
	}
}
function toggleAllTreeItem(el) {
	var treeItem=$(el).parent().find("> a");
	if (!treeItem.hasClass("treesel")) {
		treeItem.trigger("click");
		$(el).parent().find("> ul").find("a[id]").not(".treesel").trigger("click");
	} else {
		treeItem.trigger("click");
		$(el).parent().find("> ul").find("a[id]").has(".treesel").trigger("click");
	}
}

function openAllTreeItem(el) {
	if (!el) {
		if ($("#openAllBtn").attr("disabled")) return;
		$("#openAllBtn").attr("disabled",true).html("["+i18n["please.wait"]+"]");
		window.setTimeout(function() {
			$("#tree > li > ul").find("a[id]").not(".treesel").trigger("click");
			$("#openAllBtn").removeAttr("disabled").html("["+i18n["open.all"]+"]");
			},100);		
	} else {
		var treeItem=$(el).parent().find("> a");
		treeItem.trigger("click");
		$(el).parent().find("> ul").find("a[id]").not(".treesel").trigger("click");
	}
}

function closeAllTreeItem(el) {
	if (!el) {
		if ($("#closeAllBtn").attr("disabled")) return;
		$("#closeAllBtn").attr("disabled",true).html("["+i18n["please.wait"]+"]");
		window.setTimeout(function() {
			$("#tree > li > a[id].treesel").trigger("click");
			$("#closeAllBtn").removeAttr("disabled").html("["+i18n["close.all"]+"]");
			},100);		
	} else {
		var treeItem=$(el).parent().find("> a");
		treeItem.trigger("click");
		$(el).parent().find("> ul > li > a[id].treesel").trigger("click");
	}
}

function createLink(el) {	
	var loc=parent.location.href;
	if (window.location.href==loc) {
	 	prompt(i18n["direct.link"],loc.replace(/#.*$/g,"")+"#"+$(el).parent().find(">a[id]").attr("id"));
	} else {
	 	prompt(i18n["direct.link"],loc.replace(/&tid=.*$/g,"")+"#"+$(el).parent().find(">a[id]").attr("id"));
	}
}
