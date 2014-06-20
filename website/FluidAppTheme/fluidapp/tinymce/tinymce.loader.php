<?php

/*-----------------------------------------------------------------------------------*/
/*	Paths Defenitions
/*-----------------------------------------------------------------------------------*/

define('TZ_TINYMCE_PATH', FILEPATH . '/tinymce');
define('TZ_TINYMCE_URI', DIRECTORY . '/tinymce');


/*-----------------------------------------------------------------------------------*/
/*	Load TinyMCE dialog
/*-----------------------------------------------------------------------------------*/

require_once( TZ_TINYMCE_PATH . '/tinymce.class.php' );		// TinyMCE wrapper class
new tz_tinymce();											// do the magic

?>