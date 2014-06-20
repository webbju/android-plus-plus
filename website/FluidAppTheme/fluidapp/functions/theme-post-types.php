<?php

function t2t_build_taxonomies() {
	//register_taxonomy( 'gallery_albums', 't2t_gallery', array( 'hierarchical' => true, 'label' => 'Albums', 'query_var' => 'albums', 'rewrite' => true ) );	
}

add_action( 'init', 't2t_build_taxonomies', 0 );

function t2t_post_types() {
	
	// Features
	$labels = array(
		'name' => __( 'Features', 'framework'),
		'singular_name' => __( 'Feature', 'framework' ),
		'add_new' => __('Add New Feature', 'framework'),
		'add_new_item' => __('Add New Feature', 'framework'),
		'edit_item' => __('Edit Feature', 'framework'),
		'new_item' => __('New Feature', 'framework'),
		'view_item' => __('View Feature', 'framework'),
		'search_items' => __('Search Features', 'framework'),
		'not_found' =>  __('No features found', 'framework'),
		'not_found_in_trash' => __('No features found in Trash', 'framework'), 
		'parent_item_colon' => ''
	 );
	
	$args = array(
	  'labels' => $labels,
	  'public' => true,
	  'publicly_queryable' => true,
	  'show_ui' => true,
	  'exclude_from_search' => true,
	  'query_var' => true,
	  'rewrite' => array( 'slug' => __('feature', 'framework') ),
	  'capability_type' => 'post',
	  'hierarchical' => false,
	  'menu_position' => 20,
	  'supports' => array('title','editor','thumbnail', 'comments'),
	);
	register_post_type(__('t2t_features', 'framework'), $args);
	
	// Screenshots
	$labels = array(
		'name' => __( 'Screenshots', 'framework'),
		'singular_name' => __( 'Screenshot', 'framework' ),
		'add_new' => __('Add New', 'framework'),
		'add_new_item' => __('Add New Screenshot', 'framework'),
		'edit_item' => __('Edit Screenshot', 'framework'),
		'new_item' => __('New Screenshot', 'framework'),
		'view_item' => __('View Screenshot', 'framework'),
		'search_items' => __('Search Screenshots', 'framework'),
		'not_found' =>  __('No screenshots found', 'framework'),
		'not_found_in_trash' => __('No screenshots found in Trash', 'framework'), 
		'parent_item_colon' => ''
	 );
	
	$args = array(
	  'labels' => $labels,
	  'public' => true,
	  'publicly_queryable' => true,
	  'show_ui' => true,
	  'exclude_from_search' => true,
	  'query_var' => true,
	  'rewrite' => array( 'slug' => __('screenshot', 'framework') ),
	  'capability_type' => 'post',
	  'hierarchical' => false,
	  'menu_position' => 21,
	  'supports' => array('title','thumbnail'),
	);
	register_post_type(__('t2t_screenshots', 'framework'), $args);
	
	// Updates
	$labels = array(
		'name' => __( 'Updates', 'framework'),
		'singular_name' => __( 'Update', 'framework' ),
		'add_new' => __('Add New', 'framework'),
		'add_new_item' => __('Add New Update', 'framework'),
		'edit_item' => __('Edit Update', 'framework'),
		'new_item' => __('New Update', 'framework'),
		'view_item' => __('View Update', 'framework'),
		'search_items' => __('Search Update', 'framework'),
		'not_found' =>  __('No updates found', 'framework'),
		'not_found_in_trash' => __('No updates found in Trash', 'framework'), 
		'parent_item_colon' => ''
	 );
	
	$args = array(
	  'labels' => $labels,
	  'public' => true,
	  'publicly_queryable' => true,
	  'show_ui' => true,
	  'exclude_from_search' => true,
	  'query_var' => true,
	  'rewrite' => array( 'slug' => __('update', 'framework') ),
	  'capability_type' => 'post',
	  'hierarchical' => false,
	  'menu_position' => 22,
	  'supports' => array('title','editor', 'comments'),
	);
	register_post_type(__('t2t_updates', 'framework'), $args);
	
	// Press
	$labels = array(
		'name' => __( 'Press', 'framework'),
		'singular_name' => __( 'Press', 'framework' ),
		'add_new' => __('Add New Press', 'framework'),
		'add_new_item' => __('Add New Press', 'framework'),
		'edit_item' => __('Edit Press', 'framework'),
		'new_item' => __('New Press', 'framework'),
		'view_item' => __('View Press', 'framework'),
		'search_items' => __('Search Press', 'framework'),
		'not_found' =>  __('No press found', 'framework'),
		'not_found_in_trash' => __('No press found in Trash', 'framework'), 
		'parent_item_colon' => ''
	 );
	
	$args = array(
	  'labels' => $labels,
	  'public' => true,
	  'publicly_queryable' => true,
	  'show_ui' => true,
	  'exclude_from_search' => true,
	  'query_var' => true,
	  'rewrite' => array( 'slug' => __('press', 'framework') ),
	  'capability_type' => 'post',
	  'hierarchical' => false,
	  'menu_position' => 23,
	  'supports' => array('title','editor','thumbnail','custom-fields'),
	);
	register_post_type(__('t2t_press', 'framework'), $args);
}

add_action( 'init', 't2t_post_types' );

/*-----------------------------------------------------------------------------------*/
/*	Add thumbnails to edit screens
/*-----------------------------------------------------------------------------------*/
if ( !function_exists('fb_AddThumbColumn') && function_exists('add_theme_support') ) {
	
	function fb_AddThumbColumn($cols) {
		$cols['thumbnail'] = __('Thumbnail', 'framework');
		return $cols;
	}
	
	function fb_AddThumbValue($column_name, $post_id) {
		$width = (int) 35;
		$height = (int) 35;
		if ( 'thumbnail' == $column_name ) {
			
			$thumbnail_id = get_post_meta( $post_id, '_thumbnail_id', true );
			
			$attachments = get_children( array('post_parent' => $post_id, 'post_type' => 'attachment', 'post_mime_type' => 'image') );
			
			if ($thumbnail_id)
				$thumb = wp_get_attachment_image( $thumbnail_id, array($width, $height), true );
			elseif ($attachments) {
				foreach ( $attachments as $attachment_id => $attachment ) {
					$thumb = wp_get_attachment_image( $attachment_id, array($width, $height), true );
				}
			}
			if ( isset($thumb) && $thumb ) {
				echo $thumb;
			} else {
				echo __('None', 'framework');
			}
	}
}

/*-----------------------------------------------------------------------------------*/
/*	Add thumbnails to these post types
/*-----------------------------------------------------------------------------------*/

// Add thumbnails to these post types
add_filter( 'manage_t2t_features_posts_columns', 'fb_AddThumbColumn' );
add_action( 'manage_t2t_features_posts_custom_column', 'fb_AddThumbValue', 10, 2 );

add_filter( 'manage_t2t_screenshots_posts_columns', 'fb_AddThumbColumn' );
add_action( 'manage_t2t_screenshots_posts_custom_column', 'fb_AddThumbValue', 10, 2 );

add_filter( 'manage_t2t_press_posts_columns', 'fb_AddThumbColumn' );
add_action( 'manage_t2t_press_posts_custom_column', 'fb_AddThumbValue', 10, 2 );

}

?>