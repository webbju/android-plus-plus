<?php

global $wpdb;

if(get_option($shortname.'_activated') != true) {
	
	if ( is_admin() && isset($_GET['activated'] ) && $pagenow == 'themes.php' ) {	
		
		// Check if the menu_order column exists;
        $query = "SHOW COLUMNS FROM $wpdb->terms 
                    LIKE 'term_order'";
        $result = $wpdb->query($query);

        if ($result == 0) {
            $query = "ALTER TABLE $wpdb->terms ADD `term_order` INT( 4 ) NULL DEFAULT '0'";
            $result = $wpdb->query($query); 
        }

		update_option( $shortname.'_activated', true);
		
		// General Settings
		update_option($shortname.'_showtagline', true);
		update_option($shortname.'_footer_copyright', 'Copyright &copy; 2013 FluidApp. All Rights Reserved.');
		
		// App Settings
		update_option($shortname.'_button_apple', "#");
		update_option($shortname.'_button_android', "#");
		
		// Slider Options
		update_option($shortname.'_autoplay_duration', "500");
		update_option($shortname.'_pause_duration', "5000");
		
		// Style Options
		update_option($shortname.'_theme_color', "Light");
		update_option($shortname.'_link_color', "#319ebc");
		update_option($shortname.'_link_hover_color', "#333333");
		
		// Social Settings
		update_option( $shortname.'_social_twitter', '#');
		update_option( $shortname.'_social_facebook', '#');
		update_option( $shortname.'_social_vimeo', '#');
	}
}

?>