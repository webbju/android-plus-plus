<?php
	
/*-----------------------------------------------------------------------------------

	Below we have all of the custom functions for the theme
	Please be extremely cautious editing this file!
	
-----------------------------------------------------------------------------------*/


/*-----------------------------------------------------------------------------------*/
/*	Global theme variables
/*-----------------------------------------------------------------------------------*/
$theme = wp_get_theme();
$themename = $theme->Name;
$themeversion = $theme->Version;
$shortname = "t2t";

/*-----------------------------------------------------------------------------------*/
/*	Add Localization Support
/*-----------------------------------------------------------------------------------*/
function t2t_load_textdomain(){
	load_theme_textdomain('framework', get_template_directory() . '/languages');
}
add_action('after_setup_theme', 't2t_load_textdomain');

/*-----------------------------------------------------------------------------------*/
/*	Include various function files
/*-----------------------------------------------------------------------------------*/
define('FILEPATH', get_template_directory());
define('DIRECTORY', get_template_directory_uri());
define('THEME_FUNCTIONS', get_template_directory() . '/functions');
define('THEME_IMAGES', get_template_directory_uri() . '/images' );

// Load Theme Options
require_once(THEME_FUNCTIONS . '/theme-options.php');

// Load Theme Functions
require_once(THEME_FUNCTIONS . '/theme-functions.php');

// Load Admin Theme UI
require_once(THEME_FUNCTIONS . '/admin-ui.php');

// Load Post Types
require_once(THEME_FUNCTIONS . '/theme-post-types.php');

// Load Post Options
require_once(THEME_FUNCTIONS . '/theme-post-options.php');

// Load Activation Options
require_once(THEME_FUNCTIONS . '/admin-activation.php');

// Load Theme UI
require_once(THEME_FUNCTIONS . '/theme-ui.php');

// Load Theme Shortcodes
require_once(THEME_FUNCTIONS . '/theme-shortcodes.php');

// Load Theme Widgets
require_once(THEME_FUNCTIONS . '/theme-widgets.php');

// Load Shortcode TinyMCE Button
require_once(TEMPLATEPATH .'/tinymce/tinymce.loader.php');

// Load Google Fonts
require_once(THEME_FUNCTIONS . '/includes/google-fonts.php');

/*-----------------------------------------------------------------------------------*/
/*	Set Max Content Width
/*-----------------------------------------------------------------------------------*/

if ( ! isset( $content_width ) ) $content_width = 1000;

/*-----------------------------------------------------------------------------------*/
/*	Redirect To Theme Options Page on Activation
/*-----------------------------------------------------------------------------------*/
if (is_admin() && isset($_GET['activated'])){
	wp_redirect(admin_url("admin.php?page=functions.php"));
}

/*-----------------------------------------------------------------------------------*/
/*	If WP 3.0 or > include support for wp_nav_menu()
/*-----------------------------------------------------------------------------------*/
if ( function_exists( 'register_nav_menus' ) ) {
	register_nav_menus(
		array(
			'primary-menu' => __( $themename. ' Menu', $themename )
		)
	);
}

/*-----------------------------------------------------------------------------------*/
/*	WordPress Theme Customization
/*-----------------------------------------------------------------------------------*/

add_theme_support( 'custom-background' );

/*-----------------------------------------------------------------------------------*/
/*	Custom Gravatar Support
/*-----------------------------------------------------------------------------------*/
function t2t_custom_gravatar( $avatar_defaults ) {
    $tz_avatar = get_template_directory_uri() . '/images/gravatar.png';
    $avatar_defaults[$tz_avatar] = 'Custom Gravatar (/images/gravatar.png)';
    return $avatar_defaults;
}
add_filter( 'avatar_defaults', 't2t_custom_gravatar' );

/*-----------------------------------------------------------------------------------*/
/*	Add Comment Reply JS
/*-----------------------------------------------------------------------------------*/
function theme_queue_js(){
  if (!is_admin()){
    if ( is_singular() AND comments_open() AND (get_option('thread_comments') == 1))
      wp_enqueue_script( 'comment-reply' );
  }
}
add_action('wp_print_scripts', 'theme_queue_js');

/*-----------------------------------------------------------------------------------*/
/*	Add/configure thumbnails
/*-----------------------------------------------------------------------------------*/
if ( function_exists( 'add_theme_support' ) ) {
	add_theme_support( 'automatic-feed-links' );
	add_theme_support( 'post-thumbnails', array( 'post', 't2t_features', 't2t_screenshots', 't2t_press' ) );
	set_post_thumbnail_size( 525, 200, true );
}
if ( function_exists( 'add_image_size' ) ) { 
	add_image_size( 'feature-icon', 46, 46, true );	
	add_image_size( 'press-icon', 68, 58, true );	
	add_image_size( 'screenshot-thumb', 145, 218, true );	
}

/*-----------------------------------------------------------------------------------*/
/*	Register and load javascripts
/*-----------------------------------------------------------------------------------*/
function t2t_register_js() {
	if (!is_admin()) {
		wp_register_script('html5shiv', get_template_directory_uri() . '/javascripts/html5shiv.js', 'jquery');
		wp_register_script('tipsy', get_template_directory_uri() . '/javascripts/jquery.tipsy.js', 'jquery');
		wp_register_script('fancybox', get_template_directory_uri() . '/javascripts/fancybox/jquery.fancybox-1.3.4.pack.js', 'jquery');
		wp_register_script('easing', get_template_directory_uri() . '/javascripts/fancybox/jquery.easing-1.3.pack.js', 'jquery');
		wp_register_script('touchswipe', get_template_directory_uri() . '/javascripts/jquery.touchSwipe.js', 'jquery');
		wp_register_script('mobilemenu', get_template_directory_uri() . '/javascripts/jquery.mobilemenu.js', 'jquery');
		wp_register_script('infieldlabel', get_template_directory_uri() . '/javascripts/jquery.infieldlabel.js', 'jquery');
		wp_register_script('echo-slider', get_template_directory_uri() . '/javascripts/jquery.echoslider.js', 'jquery');
		wp_register_script('fluidapp', get_template_directory_uri() . '/javascripts/fluidapp.js', 'jquery');
		
		wp_enqueue_script('jquery');
		wp_enqueue_script('html5shiv');
		wp_enqueue_script('tipsy');
		wp_enqueue_script('fancybox');
		wp_enqueue_script('easing');
		wp_enqueue_script('touchswipe');
		wp_enqueue_script('mobilemenu');
		wp_enqueue_script('infieldlabel');
		wp_enqueue_script('echo-slider');
		wp_enqueue_script('fluidapp');
		
		$fonts = array();
	
		// Add fonts to this array to be preloaded
		$fonts[] = "Pacifico|Quicksand:400,700,600,500";
		if(get_option('t2t_logo_font') && get_option('t2t_logo_font_variant')) {
			$fonts[] = get_option('t2t_logo_font').':'.get_option('t2t_logo_font_variant');
		}
		if(get_option('t2t_tagline_font') && get_option('t2t_tagline_font_variant')) {
			$fonts[] = get_option('t2t_tagline_font').':'.get_option('t2t_tagline_font_variant');
		}
		if(get_option('t2t_promo_font') && get_option('t2t_promo_font_variant')) {
			$fonts[] = get_option('t2t_promo_font').':'.get_option('t2t_promo_font_variant');
		}
		if(get_option('t2t_h1_headings_font') && get_option('t2t_h1_headings_font_variant')) {
			$fonts[] = get_option('t2t_h1_headings_font').':'.get_option('t2t_h1_headings_font_variant');
		}
		if(get_option('t2t_h2_headings_font') && get_option('t2t_h2_headings_font_variant')) {
			$fonts[] = get_option('t2t_h2_headings_font').':'.get_option('t2t_h2_headings_font_variant');
		}
		if(get_option('t2t_h3_headings_font') && get_option('t2t_h3_headings_font_variant')) {
			$fonts[] = get_option('t2t_h3_headings_font').':'.get_option('t2t_h3_headings_font_variant');
		}
		$font_list = implode('|', $fonts);
	
		wp_enqueue_style("google-fonts", 'http://fonts.googleapis.com/css?family='.$font_list, false, false, "all");
		
	}
}
add_action('init', 't2t_register_js');

function t2t_login_enqueue_scripts(){
	wp_dequeue_script( 'infieldlabel' );
}
add_action( 'login_enqueue_scripts', 't2t_login_enqueue_scripts' );

/*-----------------------------------------------------------------------------------*/
/*	Register and load css
/*-----------------------------------------------------------------------------------*/
function t2t_register_css() {
	if (!is_admin()) {
		wp_register_style('style', get_stylesheet_uri(), array());
		wp_register_style('dark', get_template_directory_uri() . '/stylesheets/dark.css', array());
		wp_register_style('media-queries', get_template_directory_uri() . '/stylesheets/media.queries.css', array());
		wp_register_style('tipsy', get_template_directory_uri() . '/stylesheets/tipsy.css', array());
		wp_register_style('fancybox', get_template_directory_uri() . '/javascripts/fancybox/jquery.fancybox-1.3.4.css', array());
		wp_register_style('custom', get_template_directory_uri() . '/stylesheets/custom.php', array());
		
		wp_enqueue_style('style');
		if(get_option('t2t_theme_color') == "Dark") {
			wp_enqueue_style('dark');
		}
		wp_enqueue_style('media-queries');
		wp_enqueue_style('tipsy');
		wp_enqueue_style('fancybox');
		wp_enqueue_style('custom');
		
	}
}
add_action('wp_enqueue_scripts', 't2t_register_css');

/*-----------------------------------------------------------------------------------*/
/*	Get google font properties
/*-----------------------------------------------------------------------------------*/
function t2t_google_fonts_ajax() {
	global $wpdb; 
	
	$font_list = t2t_get_google_fonts();
	$font_family = $_GET['font_family'];
	
	$result = multidimensional_search($font_list, array('family' => $font_family));
	
	echo $result['variants'];
	die();
}

add_action('wp_ajax_t2t_google_fonts_ajax', 't2t_google_fonts_ajax');

/*-----------------------------------------------------------------------------------*/
/*	Ajax file uploading
/*-----------------------------------------------------------------------------------*/
function tz_ajax_callback() {
	global $wpdb; // this is how you get access to the database
	$save_type = $_POST['type'];
	//Uploads
	if($save_type == 'upload'){
		
		$clickedID = $_POST['data']; // Acts as the name
		$filename = $_FILES[$clickedID];
       	$filename['name'] = preg_replace('/[^a-zA-Z0-9._\-]/', '', $filename['name']); 
		
		$override['test_form'] = false;
		$override['action'] = 'wp_handle_upload';    
		$uploaded_file = wp_handle_upload($filename,$override);
		
		$attachment = array(
		'post_title' => $filename['name'],
		'post_content' => '',
		'post_type' => 'attachment',
		'post_mime_type' => $filename['type'],
		'guid' => $uploaded_file['url']
		);

		$id = wp_insert_attachment( $attachment,$uploaded_file['file']);
		wp_update_attachment_metadata( $id, wp_generate_attachment_metadata( $id, $uploaded_file['file'] ) );
		 
		$upload_tracking[] = $clickedID;
		update_option( $clickedID , $uploaded_file['url'] );
				
		 if(!empty($uploaded_file['error'])) {echo 'Upload Error: ' . $uploaded_file['error']; }	
		 else { echo $uploaded_file['url']; } // Is the Response
	}
	elseif($save_type == 'image_reset'){
			
			$id = $_POST['data']; // Acts as the name
			global $wpdb;
			$query = "DELETE FROM $wpdb->options WHERE option_name LIKE '$id'";
			$wpdb->query($query);
	
	}
	die();
}

add_action('wp_ajax_tz_ajax_post_action', 'tz_ajax_callback');

/*-----------------------------------------------------------------------------------*/
/*	Add Theme Admin Functions
/*-----------------------------------------------------------------------------------*/
function t2t_add_admin() {

global $themename, $themeversion, $shortname, $options;

if (isset($_GET['page']) && $_GET['page'] == basename(__FILE__) ) {

	if ( isset($_REQUEST['action']) && $_REQUEST['action'] == 'save' ) {

		foreach ($options as $value) {
			if(isset($value['id'])) {
				if( isset( $_REQUEST[ $value['id'] ] ) ) { 
					update_option( $value['id'], stripslashes($_REQUEST[ $value['id'] ])  ); 
				} else { 
					delete_option( $value['id'] ); 
				}
			}
		}
		header("Location: admin.php?page=functions.php&saved=true");
		die;

}
else if(isset($_REQUEST['action']) && $_REQUEST['action'] == 'reset' ) {
	foreach ($options as $value) {
		delete_option( $value['id'] ); }

	header("Location: admin.php?page=functions.php&reset=true");
die;
}
}

add_menu_page($themename, $themename, 'administrator', basename(__FILE__), 't2t_admin', get_template_directory_uri().'/functions/images/admin_icon.png');
}

/*-----------------------------------------------------------------------------------*/
/*	Initialize admin scripts
/*-----------------------------------------------------------------------------------*/
function t2t_add_init() {
	$file_dir=get_bloginfo('template_directory');
	wp_enqueue_style("t2t_colorpicker", $file_dir."/functions/stylesheets/colorpicker.css", false, "1.0", "all");
	wp_enqueue_style("functions", $file_dir."/functions/stylesheets/t2t_admin.css", false, "1.0", "all");
	wp_enqueue_script("t2t_colorpicker", $file_dir."/functions/javascripts/colorpicker.js", false, "1.0");
	wp_enqueue_script("t2t_admin", $file_dir."/functions/javascripts/t2t_admin.js", false, "1.0");
	wp_enqueue_script("ajaxupload", $file_dir."/functions/javascripts/ajaxupload.js", false, "1.0");
	wp_enqueue_script('jquery-ui-tabs');
	wp_enqueue_script('jquery-ui-sortable');
}

function t2t_admin_print_scripts($hook) {
	global $page_handle, $shortname;
	$nonce = wp_create_nonce('sidebar_rm');
		
	echo '<script type="text/javascript">
	//<![CDATA[
	var $removeSidebarURL = "' .admin_url('admin-ajax.php'). '";
	var $ajaxNonce = "' .$nonce. '";
	var $themeshortname = "'.$shortname.'";
	//]]></script>';
}

add_action('admin_print_scripts', 't2t_admin_print_scripts');
add_action('admin_init', 't2t_add_init');
add_action('admin_menu', 't2t_add_admin');


/*-----------------------------------------------------------------------------------*/
/*	Add menu item to admin bar
/*-----------------------------------------------------------------------------------*/
function t2t_admin_link() {
	global $wp_admin_bar, $wpdb, $themename;
	if ( !is_super_admin() || !is_admin_bar_showing() )
		return;
	$wp_admin_bar->add_menu( array( 'id' => 't2t_admin', 'title' => __( $themename, 'textdomain' ), 'href' => admin_url("admin.php?page=functions.php") ) );
}
add_action( 'admin_bar_menu', 't2t_admin_link', 1000 );

/*-----------------------------------------------------------------------------------*/
/*	Function for searching PHP arrays
/*-----------------------------------------------------------------------------------*/
function multidimensional_search($parents, $searched) {
  if (empty($searched) || empty($parents)) {
    return false;
  }

  foreach ($parents as $key => $value) {
    $exists = true;
    foreach ($searched as $skey => $svalue) {
      $exists = ($exists && IsSet($parents[$key][$skey]) && $parents[$key][$skey] == $svalue);
    }
    if($exists){ return $parents[$key]; }
  }

  return false;
}

/*-----------------------------------------------------------------------------------*/
/*	Change Default Excerpt Length
/*-----------------------------------------------------------------------------------*/

function t2t_excerpt_length($length) {
	return 28; 
}
add_filter('excerpt_length', 't2t_excerpt_length', 999);

/*-----------------------------------------------------------------------------------*/
/*	Configure Excerpt String
/*-----------------------------------------------------------------------------------*/

function t2t_excerpt_more($excerpt) {
	return str_replace('[...]', '...', $excerpt); 
}
add_filter('wp_trim_excerpt', 't2t_excerpt_more');


function t2t_more_link( $more_link, $more_link_text ) {
	$custom_more = "Read more...";
	return str_replace( $more_link_text, $custom_more, $more_link );
}
add_filter( 'the_content_more_link', 't2t_more_link', 10, 2 );

?>