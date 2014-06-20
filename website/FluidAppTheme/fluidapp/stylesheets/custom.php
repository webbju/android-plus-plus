<?php 
header("Content-type: text/css");

if(file_exists('../../../../wp-load.php')) :
	include '../../../../wp-load.php';
else:
	include '../../../../../wp-load.php';
endif; 

ob_flush(); 
?>

/*--------------------------------------------
Sets the link colors of the theme
---------------------------------------------*/

<?php 
$link = get_option('t2t_link_color');
$link_hover = get_option('t2t_link_hover_color');
?>

<?php if(get_option('t2t_logo_font')) { ?>
section #app_info .text a {
	font-family: "<?php echo get_option('t2t_logo_font'); ?>";
	font-size: <?php echo get_option('t2t_logo_font_size'); ?>;
	color: <?php echo get_option('t2t_logo_font_color'); ?>;
	font-weight: <?php echo get_option('t2t_logo_font_variant'); ?>;
}
<?php } ?>

<?php if(get_option('t2t_tagline_font')) { ?>
section #app_info span.tagline {
	font-family: "<?php echo get_option('t2t_tagline_font'); ?>";
	font-size: <?php echo get_option('t2t_tagline_font_size'); ?>;
	color: <?php echo get_option('t2t_tagline_font_color'); ?>;
	font-weight: <?php echo get_option('t2t_tagline_font_variant'); ?>;
}
<?php } ?>

<?php if(get_option('t2t_promo_font')) { ?>
section #app_info .price p {
	font-family: "<?php echo get_option('t2t_promo_font'); ?>";
	font-size: <?php echo get_option('t2t_promo_font_size'); ?>;
	color: <?php echo get_option('t2t_promo_font_color'); ?>;
	font-weight: <?php echo get_option('t2t_promo_font_variant'); ?>;
}
<?php } ?>

<?php if(get_option('t2t_h1_headings_font')) { ?>
h1 {
	font-family: "<?php echo get_option('t2t_h1_headings_font'); ?>";
	font-size: <?php echo get_option('t2t_h1_headings_font_size'); ?>;
	color: <?php echo get_option('t2t_h1_headings_font_color'); ?>;
	font-weight: <?php echo get_option('t2t_h1_headings_font_variant'); ?>;
}
<?php } ?>

<?php if(get_option('t2t_h2_headings_font')) { ?>
h2 {
	font-family: "<?php echo get_option('t2t_h2_headings_font'); ?>";
	font-size: <?php echo get_option('t2t_h2_headings_font_size'); ?>;
	color: <?php echo get_option('t2t_h2_headings_font_color'); ?>;
	font-weight: <?php echo get_option('t2t_h2_headings_font_variant'); ?>;
}
	h2 .icon{
		font-size: <?php echo get_option('t2t_h2_headings_font_size'); ?>;
	}
<?php } ?>

<?php if(get_option('t2t_h3_headings_font')) { ?>
h3 {
	font-family: "<?php echo get_option('t2t_h3_headings_font'); ?>";
	font-size: <?php echo get_option('t2t_h3_headings_font_size'); ?>;
	color: <?php echo get_option('t2t_h3_headings_font_color'); ?>;
	font-weight: <?php echo get_option('t2t_h3_headings_font_variant'); ?>;
}
<?php } ?>

a {
	color: <?php echo $link; ?>;
}
a:hover {
	color: <?php echo $link_hover; ?>;
}

::selection { background: <?php echo $link_hover; ?>; color: #fff; }

::-moz-selection { background: <?php echo $link_hover; ?>; color: #fff; }

<?php ob_end_flush(); ?>