<?php

// loads wordpress
require_once('get_wp.php'); // loads wordpress stuff

// gets shortcode
$shortcode = base64_decode( trim( $_GET['sc'] ) );

?>
<!DOCTYPE HTML>
<html lang="en">
<head>
<link rel="stylesheet" type="text/css" href="<?php bloginfo('stylesheet_url'); ?>" media="all" />
<?php wp_head(); ?>
<style type="text/css">
html {
	margin: 0 !important;
}
body {
	padding: 20px 15px;
}
ul.update_list { 
	padding-top: 0px !important;
}
</style>
</head>
<body>
<?php 
	
	$output = str_replace("[raw]", "", do_shortcode( $shortcode ));
	$output = str_replace("[/raw]", "", $output);
	echo $output; 
	
?>
</body>
</html>