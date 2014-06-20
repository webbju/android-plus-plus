<?php
/*-----------------------------------------------------------------------------------*/
/* Add Favicon
/*-----------------------------------------------------------------------------------*/

function t2t_favicon() {
	if (get_option('t2t_favicon') != '') {
		echo '<link rel="shortcut icon" href="'. get_option('t2t_favicon') .'"/>'."\n";
	}
}

add_action('wp_head', 't2t_favicon');

/*-----------------------------------------------------------------------------------*/
/* Show analytics code in footer */
/*-----------------------------------------------------------------------------------*/

function t2t_analytics(){
	$output = get_option('t2t_analytics_code');
	if ( $output <> "" ) 
		echo stripslashes($output) . "\n";
}
add_action('wp_footer','t2t_analytics');

?>