/**
 * jQuery Cookie plugin
 *
 * Copyright (c) 2010 Klaus Hartl (stilbuero.de)
 * Dual licensed under the MIT and GPL licenses:
 * http://www.opensource.org/licenses/mit-license.php
 * http://www.gnu.org/licenses/gpl.html
 *
 */
jQuery.cookie = function (key, value, options) {

    // key and at least value given, set cookie...
    if (arguments.length > 1 && String(value) !== "[object Object]") {
        options = jQuery.extend({}, options);

        if (value === null || value === undefined) {
            options.expires = -1;
        }

        if (typeof options.expires === 'number') {
            var days = options.expires, t = options.expires = new Date();
            t.setDate(t.getDate() + days);
        }

        value = String(value);

        return (document.cookie = [
            encodeURIComponent(key), '=',
            options.raw ? value : encodeURIComponent(value),
            options.expires ? '; expires=' + options.expires.toUTCString() : '', // use expires attribute, max-age is not supported by IE
            options.path ? '; path=' + options.path : '',
            options.domain ? '; domain=' + options.domain : '',
            options.secure ? '; secure' : ''
        ].join(''));
    }

    // key and possibly options given, get cookie...
    options = value || {};
    var result, decode = options.raw ? function (s) { return s; } : decodeURIComponent;
    return (result = new RegExp('(?:^|; )' + encodeURIComponent(key) + '=([^;]*)').exec(document.cookie)) ? decode(result[1]) : null;
};


jQuery.noConflict();

function toggle_slider_options(t_type) {
	if(jQuery("#"+$themeshortname+"_disable_slider").is(":checked")) {
		jQuery("#"+$themeshortname+"_slider_animation").parent().parent().hide();
		jQuery("#"+$themeshortname+"_autoplay_duration").parent().parent().hide();
		jQuery("#"+$themeshortname+"_pause_duration").parent().parent().hide();
		jQuery("#"+$themeshortname+"_disable_easing").parent().parent().hide();
		jQuery(".slider_options").hide().first().show();
	} else {
		jQuery("#"+$themeshortname+"_slider_animation").parent().parent().show();
		jQuery("#"+$themeshortname+"_autoplay_duration").parent().parent().show();
		jQuery("#"+$themeshortname+"_pause_duration").parent().parent().show();
		jQuery("#"+$themeshortname+"_disable_easing").parent().parent().show();
		jQuery(".slider_options").show();
	}
}

function set_slider_options() {
	toggle_slider_options();
	jQuery("#"+$themeshortname+"_disable_slider").change(function() {
		toggle_slider_options();
	});
}

function set_logo_hint(val) {
		jQuery(".logo_type > div").hide();
		if(val == "upload") {
			jQuery(".logo_type .upload").show();
		} else if(val == "text") {
			jQuery(".logo_type .text").show();
		}
}

function switch_logo_type() {
	set_logo_hint(jQuery(".logo_type input.radio:checked").val());
	jQuery(".logo_type input.radio").change(function() {
		set_logo_hint(jQuery(this).val());
	});
}

function getFontVariants(font, selected_variant) {
	var variants_select = font.parent().find(".tyopgraphy_font_variants");
	
	jQuery.ajax({
		url: ajaxurl,
		data: {
			'action' : 't2t_google_fonts_ajax',
			'font_family' : font.val()
		},
		success: function(data) {
			
			variants_select.find("option").remove();
			var variants = data.split("|");

			var i;

			for(i = 0; i < variants.length; ++i) {
					if(selected_variant == variants[i]) { var selected = "selected"; } else { var selected = ""; }
			    variants_select.append('<option value="'+variants[i]+'" '+selected+'>'+variants[i]+'</option>');
			}
			
			variants_select.show().trigger("change");
		}
	});
}

function setFontStylesheet(stylesheet, stylesheet_id) {
	if(jQuery('.'+stylesheet_id).length == 0) {
		jQuery('head').append('<link class="'+stylesheet_id+'" rel="stylesheet" type="text/css" href="'+stylesheet+'" />');
	} else {
		jQuery('.'+stylesheet_id).attr("href", stylesheet);
	}
}

jQuery(document).ready(function() {
	jQuery("#t2t_container #main").tabs({ cookie: { expires: 30 }, fx: { opacity: 'toggle', duration: 'fast' } });
	setTimeout(function() { jQuery(".fade").fadeOut(800); }, 2000);
	switch_logo_type();
	set_slider_options();
	
	// Color Picker
	jQuery('.colorSelector').each(function(){
		var Othis = this; //cache a copy of the this variable for use inside nested function
		var initialColor = jQuery(Othis).next('input').attr('value');
		jQuery(this).ColorPicker({
		color: initialColor,
		onShow: function (colpkr) {
		jQuery(colpkr).fadeIn(500);
		return false;
		},
		onHide: function (colpkr) {
		jQuery(colpkr).fadeOut(500);
		return false;
		},
		onChange: function (hsb, hex, rgb) {
		jQuery(Othis).children('div').css('backgroundColor', '#' + hex);
		jQuery(Othis).next('input').attr('value','#' + hex);
		if(jQuery(Othis).attr("id") == $themeshortname+"_logo_color_picker") { 
			jQuery("#"+$themeshortname+"_logo_color").val('#' + hex);
		} else {
			jQuery(Othis).parent().parent().parent().find(".typography_color").val('#' + hex);
			jQuery(Othis).parent().parent().find(".typography_preview div").css('color', '#' + hex);
		}
	}
	});
	}); 
	
	jQuery('#t2t_container input, #t2t_container select,#t2t_container textarea').live('change', function(e){
		if(jQuery(this).attr("class") != "tyopgraphy_font_variants") {
			jQuery('.save_bar_top').addClass('formChanged');
			jQuery('.formState').fadeIn( 400 );
		}
	});
	
	jQuery(".save_bar_top .button-primary").click(function() {
		jQuery('.save_bar_top').removeClass('formChanged');
		jQuery('.formState').hide();
	});
	
	jQuery("#"+$themeshortname+"_logo_text_trigger").keyup(function() {
		jQuery("#"+$themeshortname+"_logo_text").val(jQuery(this).val());
	});
	
	jQuery("#"+$themeshortname+"_logo_size_trigger").change(function() {
		jQuery("#"+$themeshortname+"_logo_size").val(jQuery(this).val());
	});
	
	jQuery(".logo_type_radio").change(function() {
		jQuery("#"+$themeshortname+"_logo_type").val(jQuery(this).val());
	});
	
	jQuery(".tyopgraphy_size_trigger").change(function() {
		jQuery(this).parent().parent().parent().find(".typography_size").val(jQuery(this).val());
		jQuery(this).parent().parent().parent().find(".typography_preview div").css('font-size', jQuery(this).val());
	});
	
	jQuery(".tyopgraphy_font_variants").change(function() {
		jQuery(this).parent().parent().parent().find(".typography_variant").val(jQuery(this).val());
		var stylesheet_id = jQuery(this).parent().find(".tyopgraphy_font_trigger").attr("id")+'-font';
		var base_stylesheet = jQuery('.'+stylesheet_id).attr("href").split(":");
		var stylesheet = 'http:'+base_stylesheet[1]+':'+jQuery(this).val()
		jQuery(this).parent().parent().parent().find(".typography_preview div").css('font-weight', jQuery(this).val());
		
		setFontStylesheet(stylesheet, stylesheet_id);
	});

	jQuery(".tyopgraphy_font_trigger").change(function() {
		jQuery(this).parent().parent().parent().find(".typography_preview div").css('font-family', '"'+jQuery(this).val()+'"');
		//jQuery("#t2t_fonts-css").attr("href", "http://fonts.googleapis.com/css?family="+jQuery(this).val());
		var stylesheet = 'http://fonts.googleapis.com/css?family='+jQuery(this).val();
		var stylesheet_id = jQuery(this).attr("id")+'-font';
		
		setFontStylesheet(stylesheet, stylesheet_id);
		
		getFontVariants(jQuery(this));
				
	});
	
	jQuery("select.background").change(function() {
		if(jQuery(this).val() == 'iPad (black)' || jQuery(this).val() == 'iPad (white)') {
			jQuery(this).parent().parent().parent().next(".sub_group").hide();
		} else {
			jQuery(this).parent().parent().parent().next(".sub_group").show();
		}
	});
	
});


function to_change_taxonomy(element) {
    //select the default category (0)
    jQuery('#to_form #cat').val(jQuery("#to_form #cat option:first").val());
    jQuery('#to_form').submit();
}
// For custom posts ordering
function serialize(mixed_value) {
    var _utf8Size = function (str) {
        var size = 0,
            i = 0,
            l = str.length,
            code = '';
        for (i = 0; i < l; i++) {
            code = str.charCodeAt(i);
            if (code < 0x0080) {
                size += 1;
            } else if (code < 0x0800) {
                size += 2;
            } else {
                size += 3;
            }
        }
        return size;
    };
    var _getType = function (inp) {
        var type = typeof inp,
            match;
        var key;

        if (type === 'object' && !inp) {
            return 'null';
        }
        if (type === "object") {
            if (!inp.constructor) {
                return 'object';
            }
            var cons = inp.constructor.toString();
            match = cons.match(/(\w+)\(/);
            if (match) {
                cons = match[1].toLowerCase();
            }
            var types = ["boolean", "number", "string", "array"];
            for (key in types) {
                if (cons == types[key]) {
                    type = types[key];
                    break;
                }
            }
        }
        return type;
    };
    var type = _getType(mixed_value);
    var val, ktype = '';

    switch (type) {
    case "function":
        val = "";
        break;
    case "boolean":
        val = "b:" + (mixed_value ? "1" : "0");
        break;
    case "number":
        val = (Math.round(mixed_value) == mixed_value ? "i" : "d") + ":" + mixed_value;
        break;
    case "string":
        val = "s:" + _utf8Size(mixed_value) + ":\"" + mixed_value + "\"";
        break;
    case "array":
    case "object":
        val = "a";
/*
            if (type == "object") {
                var objname = mixed_value.constructor.toString().match(/(\w+)\(\)/);
                if (objname == undefined) {
                    return;
                }
                objname[1] = this.serialize(objname[1]);
                val = "O" + objname[1].substring(1, objname[1].length - 1);
            }
            */
        var count = 0;
        var vals = "";
        var okey;
        var key;
        for (key in mixed_value) {
            if (mixed_value.hasOwnProperty(key)) {
                ktype = _getType(mixed_value[key]);
                if (ktype === "function") {
                    continue;
                }

                okey = (key.match(/^[0-9]+$/) ? parseInt(key, 10) : key);
                vals += this.serialize(okey) + this.serialize(mixed_value[key]);
                count++;
            }
        }
        val += ":" + count + ":{" + vals + "}";
        break;
    case "undefined":
        // Fall-through
    default:
        // if the JS object has a property which contains a null value, the string cannot be unserialized by PHP
        val = "N";
        break;
    }
    if (type !== "object" && type !== "array") {
        val += ";";
    }
    return val;
}