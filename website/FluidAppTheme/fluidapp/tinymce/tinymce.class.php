<?php

class tz_tinymce
{	
	function __construct()
	{
		add_action('admin_init', array( &$this, 'tz_head' ));
		add_action('init', array( &$this, 'tz_tinymce_rich_buttons' ));
		add_action('admin_print_scripts', array( &$this, 'tz_quicktags' ));
	}
	
	// --------------------------------------------------------------------------
	
	function tz_head()
	{
		// css
		wp_enqueue_style( 'tz-popup', TZ_TINYMCE_URI . '/css/popup.css', false, '1.0', 'all' );
		
		// js
		wp_enqueue_script('jquery-ui-sortable');
		wp_enqueue_script( 'jquery-livequery', TZ_TINYMCE_URI . '/js/jquery.livequery.js', false, '1.1.1', false );
		wp_enqueue_script( 'jquery-appendo', TZ_TINYMCE_URI . '/js/jquery.appendo.js', false, '1.0', false );
		wp_enqueue_script( 'base64', TZ_TINYMCE_URI . '/js/base64.js', false, '1.0', false );
		wp_enqueue_script( 'tz-popup', TZ_TINYMCE_URI . '/js/popup.js', false, '1.0', false );
	}
	
	// --------------------------------------------------------------------------
	
	/**
	 * Registers TinyMCE rich editor buttons
	 *
	 * @return	void
	 */
	function tz_tinymce_rich_buttons()
	{
		if ( ! current_user_can('edit_posts') && ! current_user_can('edit_pages') )
			return;
	
		if ( get_user_option('rich_editing') == 'true' )
		{
			add_filter( 'mce_external_plugins', array( &$this, 'tz_add_rich_plugins' ) );
			add_filter( 'mce_buttons', array( &$this, 'tz_register_rich_buttons' ) );
		}
	}
	
	// --------------------------------------------------------------------------
	
	/**
	 * Defins TinyMCE rich editor js plugin
	 *
	 * @return	void
	 */
	function tz_add_rich_plugins( $plugin_array )
	{
		$plugin_array['tzShortcodes'] = TZ_TINYMCE_URI . '/plugin.js';
		return $plugin_array;
	}
	
	// --------------------------------------------------------------------------
	
	/**
	 * Adds TinyMCE rich editor buttons
	 *
	 * @return	void
	 */
	function tz_register_rich_buttons( $buttons )
	{
		array_push( $buttons, "|", 'tz_button' );
		return $buttons;
	}
	
	// --------------------------------------------------------------------------
	
	/**
	 * Registers TinyMCE HTML editor quicktags buttons
	 *
	 * @return	void
	 */
	function tz_quicktags() {
		// wp_enqueue_script( 'tz_quicktags', TZ_TINYMCE_URI . '/plugins/wizylabs_quicktags.js', array('quicktags') );
	}
}

?>