jQuery(document).ready(function($) {
		
	// Enable mobile drop down navigation
	$("nav ul:first").mobileMenu();
	
	$("label").inFieldLabels({ fadeOpacity: 0.4 });
	
	// Drop down menus
	$("header nav ul li").hover(function() {
		if($(this).find("ul").size != 0) {
			$(this).find("ul:first").stop(true, true).fadeIn("fast");
		}
	}, function() {
		$(this).find("ul:first").stop(true, true).fadeOut("fast");
	});
	
	$("header nav ul li").each(function() {
		$("ul li:last a", this).css({ 'border' : 'none' });
	});
		
	// Gallery hover
	$(".screenshot_grid div").each(function() {
		$("a", this).append('<span class="hover"></span>');
	});
	
	$(".screenshot_grid div").hover(function() {
		$("a", this).find(".hover").stop(true, true).fadeIn(400);
	}, function() {
		$("a", this).find(".hover").stop(true, true).fadeOut(400);
	});
	
	// Fancyboxes
	$("a.fancybox").fancybox({
		"transitionIn":			"elastic",
		"transitionOut":		"elastic",
		"easingIn":					"easeOutBack",
		"easingOut":				"easeInBack",
		"padding":					0,
		"speedIn":      		500,
		"speedOut": 				500,
		"hideOnContentClick":	false,
		"overlayShow":        false
	});
	$("a.youtube").click(function() {
		$.fancybox({
				"transitionIn":			"elastic",
				"transitionOut":		"elastic",
				"easingIn":					"easeOutBack",
				"padding":					0,
				"speedIn":      		500,
				"speedOut": 				0,
				"hideOnContentClick":	false,
				"overlayShow":        false,
				'title'			: this.title,
				'href'			: this.href.replace(new RegExp("watch\\?v=", "i"), 'v/'),
				'type'			: 'swf',
				'swf'			: {
				  'wmode'		: 'transparent',
					'allowfullscreen'	: 'true'
				}
			});
		return false;
	});
	$("a.vimeo").click(function() {
		$.fancybox({
			"transitionIn":			"elastic",
			"transitionOut":		"elastic",
			"easingIn":					"easeOutBack",
			"padding":					0,
			"speedIn":      		500,
			"speedOut": 				0,
			"hideOnContentClick":	false,
			"overlayShow":        false,
			'title'			: this.title,
			'href' 			: this.href.replace(new RegExp("([0-9])","i"),'moogaloop.swf?clip_id=$1'),
			'type'			: 'swf'
	  });
		return false;
	});
	
	// Center nav arrow indicator
	var nav_item = $("nav ul li.current-menu-item a, ul li.current_page_parent a, ul li.current_page_item a");
	if(nav_item.length != 0) {
		var left_margin = (nav_item.parent().position().left + nav_item.parent().width()) + 24 - (nav_item.parent().width() / 2);
		$("nav .arrow").css({ left: left_margin - 8 }).show();
	}
	
	// Style comment form button
	$("#commentform #submit, #searchsubmit").addClass("button white");
	$("div#blog .post").last().addClass("last");
	
	// Shortcode fixes
	$(".team_members").each(function() {
		if($(this).hasClass("two_column")) {
			$(this).find(".person").addClass("one_half");
			$(this).find(".person:nth-child(2n)").addClass("column_last");
		}
	});
		
	// Custom jQuery Tabs
	$(".tabs").find(".pane:first").show().end().find("ul.nav li:first").addClass("current");
	$(".tabs ul.nav li a").click(function() {
		var tab_container = $(this).parent().parent().parent();
		$(this).parent().parent().find("li").removeClass("current");
		$(this).parent().addClass("current");
		$(".pane", tab_container).hide();
		$("#"+$(this).attr("class")+".pane", tab_container).show();
	});	
		
	// Toggle lists
	$(".toggle_list ul li .title").click(function() {
		var content_container = $(this).parent().find(".content");
		if(content_container.is(":visible")) {
			// var page_height = $(".page.current").height() - content_container.height();
			// 		FluidNav.resizePage(page_height, true);
			content_container.slideUp();
			$(this).find("a.toggle_link").text($(this).find("a.toggle_link").data("open_text"));
		} else {
			// var page_height = $(".page.current").height() + content_container.height() + 40;
			// FluidNav.resizePage(page_height, true);
			content_container.slideDown();
			$(this).find("a.toggle_link").text($(this).find("a.toggle_link").data("close_text"));
		}
	});
	
	$(".toggle_list ul li .title").each(function() {
		$(this).find("a.toggle_link").text($(this).find("a.toggle_link").data("open_text"));
		if($(this).parent().hasClass("opened")) {
			$(this).parent().find(".content").show();
		}
	});
		
	// Tooltips
	$("a[rel=tipsy]").tipsy({fade: true, gravity: 's', offset: 5, html: true});
	
	$("ul.social li a").each(function() {
		var title = $(this).attr("title").split("_").join(" ");
		$(this).tipsy({
				fade: true, 
				gravity: 'n', 
				offset: 0,
				title: function() {
					return title;
				}
		});
	});
	
	// Contact form
	$("div#contact_form form").submit(function() {
  	var this_form = $(this);
  	$.ajax({
  		type: 'post',
  		data: this_form.serialize(),
  		url: this_form.attr("action"),
  		success: function(res) {
  			if(res == "true") {
  				this_form.fadeOut("fast");
					$(".validation").fadeOut("fast");
					$(".success").fadeIn("fast");
  			} else {
  				$(".validation").fadeIn("fast");
  				this_form.find(".text").removeClass("error");
  				$.each(res.split(","), function() {
  					this_form.find("#"+this).addClass("error");
  				});
  			}
  		}
  	});
		return false;
  });
	
});