<?php

// Check WP version
function t2t_check_wp_version(){
	global $wp_version;
	
	$minium_WP   = '3.0';
	$wp_ok  =  version_compare($wp_version, $minium_WP, '>=');
	
	if ( ($wp_ok == FALSE) ) {
		return false;
	}
	
	return true;
}

// Define images dir URL
$url = get_template_directory_uri() . '/functions/images/';

$bg_device_list = array('iPhone 5 (black)','iPhone 5 (white)','iPhone 4S (white)','iPhone 4S (black)','Blackberry','Android','Windows','iPad (black)','iPad (white)');
$fg_device_list = array('iPhone 5 (black)','iPhone 5 (white)','iPhone 4S (white)','iPhone 4S (black)','Blackberry','Android','Windows');

$options = array (

	array( "name" => __('General Settings', 'framework'),
		"type" => "section"),
	array( "type" => "open"),

	array( "name" => __('Activated?', 'framework'),
		"id" => $shortname."_activated",
		"type" => "hidden",
		"std" => "true"),

	array( "name" => __('Logo', 'framework'),
		"desc" => __('Upload or type the full URL of your logo image here', 'framework'),
		"id" => $shortname."_logo",
		"type" => "logo",
		"std" => ""),

		array( "name" => __('Logo Text', 'framework'),
			"id" => $shortname."_logo_text",
			"type" => "hidden",
			"std" => ""),	

		array( "name" => __('Logo Type', 'framework'),
			"id" => $shortname."_logo_type",
			"type" => "hidden",
			"std" => ""),

	array( "name" => __('Show Tagline?', 'framework'),
		"desc" => __('Check this to show your site\'s tagline below the logo.', 'framework'),
		"id" => $shortname."_logo_tagline",
		"type" => "checkbox",
		"std" => ""),

	array( "name" => __('Custom Favicon', 'framework'),
		"desc" => __('Upload or type the full URL of your custom favicon image here', 'framework'),
		"id" => $shortname."_favicon",
		"type" => "upload",
		"std" => ""),

	array( "name" => __('Contact Form Email', 'framework'),
		"desc" => __('Specify where to send the contact form messages. If left blank, it will default to your Wordpress admin users email address.', 'framework'),
		"id" => $shortname."_contact_form_email",
		"type" => "text",
		"std" => ""),	

	array( "name" => __('Analytics Code', 'framework'),
		"desc" => __('Paste your Google Analytics or other tracking code here. This will be automatically added to the footer of every page.', 'framework'),
		"id" => $shortname."_analytics_code",
		"type" => "textarea",
		"std" => ""),	

	array( "name" => __('Footer Copyright Text', 'framework'),
		"desc" => __('Enter text shown in the the footer. It can be HTML.', 'framework'),
		"id" => $shortname."_footer_copyright",
		"type" => "text",
		"std" => ""),	

	array( "type" => "close"),

	// Start App Settings

	array( "name" => __('App Settings', 'framework'),
		"type" => "section"),
	array( "type" => "open"),

	array( "type" => "group_open"),

	array( "type" => "sub_heading", "name" => __('App Store Buttons', 'framework'), "desc" => __("Enter the URL's for the app store buttons you wish to display. Leave any row blank to disable that button.", 'framework')),

	array( "name" => __('Apple Button URL', 'framework'),
		"id" => $shortname."_button_apple",
		"type" => "text",
		"std" => ""),

	array( "name" => __('Apple Button Text', 'framework'),
		"id" => $shortname."_button_iphone_text",
		"type" => "text",
		"std" => "<small>Download now for</small> iPhone"),

	array( "name" => __('Android URL', 'framework'),
		"id" => $shortname."_button_android",
		"type" => "text",
		"std" => ""),

	array( "name" => __('Android Button Text', 'framework'),
		"id" => $shortname."_button_android_text",
		"type" => "text",
		"std" => "<small>Download now for</small> Android"),

	array( "name" => __('Blackberry URL', 'framework'),
		"id" => $shortname."_button_blackberry",
		"type" => "text",
		"std" => ""),

	array( "name" => __('Blackberry Button Text', 'framework'),
		"id" => $shortname."_button_blackberry_text",
		"type" => "text",
		"std" => "<small>Download now for</small> Blackberry"),

	array( "name" => __('Windows URL', 'framework'),
		"id" => $shortname."_button_windows",
		"type" => "text",
		"std" => ""),

	array( "name" => __('Windows Button Text', 'framework'),
		"id" => $shortname."_button_windows_text",
		"type" => "text",
		"std" => "<small>Download now for</small> Windows"),

	array( "type" => "group_close"),

	array( "name" => __('App Description', 'framework'),
		"id" => $shortname."_app_description",
		"type" => "textarea",
		"std" => ""),

	array( "name" => __('Promo Text', 'framework'),
		"desc" => __('Enter shown under the app store buttons. Leave blank to disable.', 'framework'),
		"id" => $shortname."_promo_text",
		"type" => "text",
		"std" => ""),	

	array( "name" => __('Promo Text Arrow Position', 'framework'),
		"id" => $shortname."_promo_text_arrow",
		"type" => "select",
		"options" => array('left','center','right','disable'),
		"std" => ""),	

	array( "type" => "close"),

	// Start Style

	array( "name" => __('Style Options', 'framework'),
		"type" => "section"),
	array( "type" => "open"),

	array( "name" => __('Theme Color', 'framework'),
		"id" => $shortname."_theme_color",
		"type" => "select",
		"options" => array('Light','Dark'),
		"std" => ""),	

	array( "name" => __('Link Color', 'framework'),
		"desc" => __('The color to use for main links. Default color is: #319ebc', 'framework'),
		"id" => $shortname."_link_color",
		"type" => "colorpicker",
		"width" => "80",
		"std" => ""),		

	array( "name" => __('Link Hover Color', 'framework'),
		"desc" => __('The color to use for main link hover. Default color is: #333333', 'framework'),
		"id" => $shortname."_link_hover_color",
		"type" => "colorpicker",
		"width" => "80",
		"std" => ""),	

	array( "name" => __('Custom CSS', 'framework'),
		"id" => $shortname."_custom_css",
		"type" => "textarea",
		"std" => ""),	

	array( "name" => __('Custom Javascript', 'framework'),
		"id" => $shortname."_custom_js",
		"type" => "textarea",
		"std" => ""),		

	array( "type" => "close"),

	// Start Slider

	array( "name" => __('Slider Options', 'framework'),
		"type" => "section"),
	array( "type" => "open"),

	array( "name" => __('Disable Slider?', 'framework'),
		"desc" => __('Check this to completely disable the homepage slider. If disabled, only the first Slide will be shown.', 'framework'),
		"id" => $shortname."_disable_slider",
		"type" => "checkbox",
		"std" => ""),

	array( "name" => __('Slider Animation', 'framework'),
		"id" => $shortname."_slider_animation",
		"type" => "select",
		"options" => array('slide','fade'),
		"std" => ""),	

	array( "name" => __('Autoplay Duration', 'framework'),
		"id" => $shortname."_autoplay_duration",
		"desc" => __('The time in milliseconds between slider transitions where 1000 = 1 second. Leave blank to disable autoplay.', 'framework'),
		"type" => "text",
		"std" => "",
		"width" => "100"),

	array( "name" => __('Pause Duration', 'framework'),
		"id" => $shortname."_pause_duration",
		"desc" => __('The time in milliseconds each slide pauses where 1000 = 1 second.', 'framework'),
		"type" => "text",
		"std" => "",
		"width" => "100"),	

	array( "name" => __('Disable Easing?', 'framework'),
		"desc" => __('Check this to disable animation easing (bouncing effect).', 'framework'),
		"id" => $shortname."_disable_easing",
		"type" => "checkbox",
		"std" => ""),

	array( "type" => "seperator"),

	array( "type" => "slider_nav"),

	array( "type" => "container_open", "id" => "slide_1", "std" => "slider_options"),
	array( "type" => "group_open"),

		array( "type" => "sub_group_open"),

			array( "type" => "sub_heading", "name" => "Background Device", "class" => "icon bg_device"),

			array( "name" => __('Device Type', 'framework'),
				"id" => $shortname."_slide_1_background_device",
				"type" => "select",
				"class" => "background",
				"options" => $bg_device_list,
				"std" => ""),

			array( "name" => __('Screenshot', 'framework'),
				"id" => $shortname."_slide_1_background_screenshot",
				"type" => "upload",
				"desc" => __('Upload a screenshot to place inside this device.', 'framework'),
				"std" => ""),

			array( "name" => __('Video URL', 'framework'),
				"id" => $shortname."_slide_1_background_video_url",
				"type" => "text",
				"desc" => __('Enter the YouTube or Vimeo page URL for your video.', 'framework'),
				"std" => ""),

		array( "type" => "sub_group_close"),

		array( "type" => "sub_group_open"),

			array( "type" => "sub_heading", "name" => "Foreground Device", "class" => "icon fg_device"),

			array( "name" => __('Device Type', 'framework'),
				"id" => $shortname."_slide_1_foreground_device",
				"type" => "select",
				"options" => $fg_device_list,
				"std" => ""),

			array( "name" => __('Screenshot', 'framework'),
				"id" => $shortname."_slide_1_foreground_screenshot",
				"type" => "upload",
				"desc" => __('Upload a screenshot to place inside this device.', 'framework'),
				"std" => ""),

			array( "name" => __('Video URL', 'framework'),
				"id" => $shortname."_slide_1_foreground_video_url",
				"type" => "text",
				"desc" => __('Enter the YouTube or Vimeo page URL for your video.', 'framework'),
				"std" => ""),

		array( "type" => "sub_group_close"),

	array( "type" => "group_close"),	
	array( "type" => "container_close"),
	array( "type" => "container_open", "id" => "slide_2", "std" => "slider_options"),

	array( "type" => "group_open"),

		array( "type" => "sub_group_open"),

			array( "type" => "sub_heading", "name" => "Background Device", "class" => "icon bg_device"),

			array( "name" => __('Device Type', 'framework'),
				"id" => $shortname."_slide_2_background_device",
				"type" => "select",
				"class" => "background",
				"options" => $bg_device_list,
				"std" => ""),

			array( "name" => __('Screenshot', 'framework'),
				"id" => $shortname."_slide_2_background_screenshot",
				"type" => "upload",
				"desc" => __('Upload a screenshot to place inside this device.', 'framework'),
				"std" => ""),

			array( "name" => __('Video URL', 'framework'),
				"id" => $shortname."_slide_2_background_video_url",
				"type" => "text",
				"desc" => __('Enter the YouTube or Vimeo page URL for your video.', 'framework'),
				"std" => ""),

		array( "type" => "sub_group_close"),

		array( "type" => "sub_group_open"),

			array( "type" => "sub_heading", "name" => "Foreground Device", "class" => "icon fg_device"),

			array( "name" => __('Device Type', 'framework'),
				"id" => $shortname."_slide_2_foreground_device",
				"type" => "select",
				"options" => $fg_device_list,
				"std" => ""),

			array( "name" => __('Screenshot', 'framework'),
				"id" => $shortname."_slide_2_foreground_screenshot",
				"type" => "upload",
				"desc" => __('Upload a screenshot to place inside this device.', 'framework'),
				"std" => ""),

			array( "name" => __('Video URL', 'framework'),
				"id" => $shortname."_slide_2_foreground_video_url",
				"type" => "text",
				"desc" => __('Enter the YouTube or Vimeo page URL for your video.', 'framework'),
				"std" => ""),

		array( "type" => "sub_group_close"),

	array( "type" => "group_close"),
	array( "type" => "container_close"),
	array( "type" => "container_open", "id" => "slide_3", "std" => "slider_options"),

	array( "type" => "group_open"),

		array( "type" => "sub_group_open"),

			array( "type" => "sub_heading", "name" => "Background Device", "class" => "icon bg_device"),

			array( "name" => __('Device Type', 'framework'),
				"id" => $shortname."_slide_3_background_device",
				"type" => "select",
				"class" => "background",
				"options" => $bg_device_list,
				"std" => ""),

			array( "name" => __('Screenshot', 'framework'),
				"id" => $shortname."_slide_3_background_screenshot",
				"type" => "upload",
				"desc" => __('Upload a screenshot to place inside this device.', 'framework'),
				"std" => ""),

			array( "name" => __('Video URL', 'framework'),
				"id" => $shortname."_slide_3_background_video_url",
				"type" => "text",
				"desc" => __('Enter the YouTube or Vimeo page URL for your video.', 'framework'),
				"std" => ""),

		array( "type" => "sub_group_close"),

		array( "type" => "sub_group_open"),

			array( "type" => "sub_heading", "name" => "Foreground Device", "class" => "icon fg_device"),

			array( "name" => __('Device Type', 'framework'),
				"id" => $shortname."_slide_3_foreground_device",
				"type" => "select",
				"options" => $fg_device_list,
				"std" => ""),

			array( "name" => __('Screenshot', 'framework'),
				"id" => $shortname."_slide_3_foreground_screenshot",
				"type" => "upload",
				"desc" => __('Upload a screenshot to place inside this device.', 'framework'),
				"std" => ""),

			array( "name" => __('Video URL', 'framework'),
				"id" => $shortname."_slide_3_foreground_video_url",
				"type" => "text",
				"desc" => __('Enter the YouTube or Vimeo page URL for your video.', 'framework'),
				"std" => ""),

		array( "type" => "sub_group_close"),

	array( "type" => "group_close"),
	array( "type" => "container_close"),
	array( "type" => "container_open", "id" => "slide_4", "std" => "slider_options"),

	array( "type" => "group_open"),

		array( "type" => "sub_group_open"),

			array( "type" => "sub_heading", "name" => "Background Device", "class" => "icon bg_device"),

			array( "name" => __('Device Type', 'framework'),
				"id" => $shortname."_slide_4_background_device",
				"type" => "select",
				"class" => "background",
				"options" => $bg_device_list,
				"std" => ""),

			array( "name" => __('Screenshot', 'framework'),
				"id" => $shortname."_slide_4_background_screenshot",
				"type" => "upload",
				"desc" => __('Upload a screenshot to place inside this device.', 'framework'),
				"std" => ""),

			array( "name" => __('Video URL', 'framework'),
				"id" => $shortname."_slide_4_background_video_url",
				"type" => "text",
				"desc" => __('Enter the YouTube or Vimeo page URL for your video.', 'framework'),
				"std" => ""),

		array( "type" => "sub_group_close"),

		array( "type" => "sub_group_open"),

			array( "type" => "sub_heading", "name" => "Foreground Device", "class" => "icon fg_device"),

			array( "name" => __('Device Type', 'framework'),
				"id" => $shortname."_slide_4_foreground_device",
				"type" => "select",
				"options" => $fg_device_list,
				"std" => ""),

			array( "name" => __('Screenshot', 'framework'),
				"id" => $shortname."_slide_4_foreground_screenshot",
				"type" => "upload",
				"desc" => __('Upload a screenshot to place inside this device.', 'framework'),
				"std" => ""),

			array( "name" => __('Video URL', 'framework'),
				"id" => $shortname."_slide_4_foreground_video_url",
				"type" => "text",
				"desc" => __('Enter the YouTube or Vimeo page URL for your video.', 'framework'),
				"std" => ""),

		array( "type" => "sub_group_close"),

	array( "type" => "group_close"),
	array( "type" => "container_close"),
	array( "type" => "container_open", "id" => "slide_5", "std" => "slider_options"),

	array( "type" => "group_open"),

		array( "type" => "sub_group_open"),

			array( "type" => "sub_heading", "name" => "Background Device", "class" => "icon bg_device"),

			array( "name" => __('Device Type', 'framework'),
				"id" => $shortname."_slide_5_background_device",
				"type" => "select",
				"class" => "background",
				"options" => $bg_device_list,
				"std" => ""),

			array( "name" => __('Screenshot', 'framework'),
				"id" => $shortname."_slide_5_background_screenshot",
				"type" => "upload",
				"desc" => __('Upload a screenshot to place inside this device.', 'framework'),
				"std" => ""),

			array( "name" => __('Video URL', 'framework'),
				"id" => $shortname."_slide_5_background_video_url",
				"type" => "text",
				"desc" => __('Enter the YouTube or Vimeo page URL for your video.', 'framework'),
				"std" => ""),

		array( "type" => "sub_group_close"),

		array( "type" => "sub_group_open"),

			array( "type" => "sub_heading", "name" => "Foreground Device", "class" => "icon fg_device"),

			array( "name" => __('Device Type', 'framework'),
				"id" => $shortname."_slide_5_foreground_device",
				"type" => "select",
				"options" => $fg_device_list,
				"std" => ""),

			array( "name" => __('Screenshot', 'framework'),
				"id" => $shortname."_slide_5_foreground_screenshot",
				"type" => "upload",
				"desc" => __('Upload a screenshot to place inside this device.', 'framework'),
				"std" => ""),

			array( "name" => __('Video URL', 'framework'),
				"id" => $shortname."_slide_5_foreground_video_url",
				"type" => "text",
				"desc" => __('Enter the YouTube or Vimeo page URL for your video.', 'framework'),
				"std" => ""),

		array( "type" => "sub_group_close"),

	array( "type" => "group_close"),
	array( "type" => "container_close"),

	array( "type" => "close"),

	// Start Typography

	array( "name" => __('Typography Options', 'framework'),
		"type" => "section"),
	array( "type" => "open"),

	array( "type" => "container_open", "std" => "logo"),

		array( "name" => __('Logo', 'framework'),
			"id" => $shortname."_logo_font",
			"type" => "google_fonts",
			"font_size" => "50",
			"font_color" => "#333333",
			"font_variant" => "regular",
			"std" => "Quicksand"),

			array( "name" => __('Logo Variant', 'framework'),
				"id" => $shortname."_logo_font_variant",
				"type" => "hidden",
				"class" => "typography_variant",
				"std" => ""),

			array( "name" => __('Logo Size', 'framework'),
				"id" => $shortname."_logo_font_size",
				"type" => "hidden",
				"class" => "typography_size",
				"std" => ""),	

			array( "name" => __('Logo Color', 'framework'),
				"id" => $shortname."_logo_font_color",
				"type" => "hidden",
				"class" => "typography_color",
				"std" => ""),

	array( "type" => "container_close"),

	array( "type" => "container_open", "std" => "tagline"),

		array( "name" => __('Tagline', 'framework'),
			"id" => $shortname."_tagline_font",
			"type" => "google_fonts",
			"font_size" => "19",
			"font_color" => "#1f1f1f",
			"font_variant" => "regular",
			"std" => "Quicksand"),

			array( "name" => __('Tagline Variant', 'framework'),
				"id" => $shortname."_tagline_font_variant",
				"type" => "hidden",
				"class" => "typography_variant",
				"std" => ""),

			array( "name" => __('Tagline Size', 'framework'),
				"id" => $shortname."_tagline_font_size",
				"type" => "hidden",
				"class" => "typography_size",
				"std" => ""),	

			array( "name" => __('Tagline Color', 'framework'),
				"id" => $shortname."_tagline_font_color",
				"type" => "hidden",
				"class" => "typography_color",
				"std" => ""),

	array( "type" => "container_close"),

	array( "type" => "container_open", "std" => "promo"),

		array( "name" => __('Promo', 'framework'),
			"id" => $shortname."_promo_font",
			"type" => "google_fonts",
			"font_size" => "20",
			"font_color" => "#1f1f1f",
			"font_variant" => "regular",
			"std" => "Nothing You Could Do"),

			array( "name" => __('Promo Variant', 'framework'),
				"id" => $shortname."_promo_font_variant",
				"type" => "hidden",
				"class" => "typography_variant",
				"std" => ""),

			array( "name" => __('Promo Size', 'framework'),
				"id" => $shortname."_promo_font_size",
				"type" => "hidden",
				"class" => "typography_size",
				"std" => ""),	

			array( "name" => __('Promo Color', 'framework'),
				"id" => $shortname."_promo_font_color",
				"type" => "hidden",
				"class" => "typography_color",
				"std" => ""),

	array( "type" => "container_close"),

	array( "type" => "container_open", "std" => "h1_headings"),

		array( "name" => __('H1 Headings', 'framework'),
			"id" => $shortname."_h1_headings_font",
			"type" => "google_fonts",
			"font_size" => "28",
			"font_color" => "#aaaaaa",
			"font_variant" => "regular",
			"std" => "Quicksand"),

			array( "name" => __('H1 Headings Variant', 'framework'),
				"id" => $shortname."_h1_headings_font_variant",
				"type" => "hidden",
				"class" => "typography_variant",
				"std" => ""),

			array( "name" => __('H1 Headings Size', 'framework'),
				"id" => $shortname."_h1_headings_font_size",
				"type" => "hidden",
				"class" => "typography_size",
				"std" => ""),	

			array( "name" => __('H1 Headings Color', 'framework'),
				"id" => $shortname."_h1_headings_font_color",
				"type" => "hidden",
				"class" => "typography_color",
				"std" => ""),

	array( "type" => "container_close"),

	array( "type" => "container_open", "std" => "h2_headings"),

		array( "name" => __('H2 Headings', 'framework'),
			"id" => $shortname."_h2_headings_font",
			"type" => "google_fonts",
			"font_size" => "24",
			"font_color" => "#353535",
			"font_variant" => "regular",
			"std" => "Philosopher"),

			array( "name" => __('H2 Headings Variant', 'framework'),
				"id" => $shortname."_h2_headings_font_variant",
				"type" => "hidden",
				"class" => "typography_variant",
				"std" => ""),

			array( "name" => __('H2 Headings Size', 'framework'),
				"id" => $shortname."_h2_headings_font_size",
				"type" => "hidden",
				"class" => "typography_size",
				"std" => ""),	

			array( "name" => __('H2 Headings Color', 'framework'),
				"id" => $shortname."_h2_headings_font_color",
				"type" => "hidden",
				"class" => "typography_color",
				"std" => ""),

	array( "type" => "container_close"),

	array( "type" => "container_open", "std" => "h3_headings"),

		array( "name" => __('H3 Headings', 'framework'),
			"id" => $shortname."_h3_headings_font",
			"type" => "google_fonts",
			"font_size" => "18",
			"font_color" => "#353535",
			"font_variant" => "regular",
			"std" => "Philosopher"),

			array( "name" => __('H3 Headings Variant', 'framework'),
				"id" => $shortname."_h3_headings_font_variant",
				"type" => "hidden",
				"class" => "typography_variant",
				"std" => ""),

			array( "name" => __('H3 Headings Size', 'framework'),
				"id" => $shortname."_h3_headings_font_size",
				"type" => "hidden",
				"class" => "typography_size",
				"std" => ""),	

			array( "name" => __('H3 Headings Color', 'framework'),
				"id" => $shortname."_h3_headings_font_color",
				"type" => "hidden",
				"class" => "typography_color",
				"std" => ""),

	array( "type" => "container_close"),

	array( "type" => "close"),

	array( "name" => __('Social Settings', 'framework'),
		"type" => "section"),
	array( "type" => "open"),

	array( "type" => "desc", "desc" => __('Add social links to the header of your site. Leave any blank to disable.', 'framework')),

	array( "name" => __('Twitter URL', 'framework'),
		"id" => $shortname."_social_twitter",
		"type" => "text",
		"std" => ""),
	
	array( "name" => __('Facebook URL', 'framework'),
		"id" => $shortname."_social_facebook",
		"type" => "text",
		"std" => ""),

	array( "name" => __('Flickr URL', 'framework'),
		"id" => $shortname."_social_flickr",
		"type" => "text",
		"std" => ""),	
		
	array( "name" => __('GitHub URL', 'framework'),
		"id" => $shortname."_social_github",
		"type" => "text",
		"std" => ""),		
	
	array( "name" => __('Vimeo URL', 'framework'),
		"id" => $shortname."_social_vimeo",
		"type" => "text",
		"std" => ""),	
	
	array( "name" => __('Google Plus URL', 'framework'),
		"id" => $shortname."_social_google_plus",
		"type" => "text",
		"std" => ""),
	
	array( "name" => __('Pinterest URL', 'framework'),
		"id" => $shortname."_social_pinterest",
		"type" => "text",
		"std" => ""),
	
	array( "name" => __('LinkedIn URL', 'framework'),
		"id" => $shortname."_social_linkedin",
		"type" => "text",
		"std" => ""),
	
	array( "name" => __('Dribbble URL', 'framework'),
		"id" => $shortname."_social_dribbble",
		"type" => "text",
		"std" => ""),
	
	array( "name" => __('StumbleUpon URL', 'framework'),
		"id" => $shortname."_social_stumbleupon",
		"type" => "text",
		"std" => ""),
	
	array( "name" => __('Last.fm URL', 'framework'),
		"id" => $shortname."_social_lastfm",
		"type" => "text",
		"std" => ""),
	
	array( "name" => __('Rdio URL', 'framework'),
		"id" => $shortname."_social_rdio",
		"type" => "text",
		"std" => ""),
	
	array( "name" => __('Spotify URL', 'framework'),
		"id" => $shortname."_social_spotify",
		"type" => "text",
		"std" => ""),
	
	array( "name" => __('Skype URL', 'framework'),
		"id" => $shortname."_social_skype",
		"type" => "text",
		"std" => ""),
	
	array( "name" => __('Info / Email', 'framework'),
		"id" => $shortname."_social_info",
		"type" => "text",
		"std" => ""),
	

	array( "type" => "close"),
	

);
?>