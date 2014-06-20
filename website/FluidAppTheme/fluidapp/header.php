<!DOCTYPE html>
<html lang="en" <?php language_attributes(); ?>>
<head>
	<title><?php bloginfo('name'); ?><?php wp_title('-'); ?></title>
	
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, minimum-scale=1.0, maximum-scale=1.0, user-scalable=no" />
	
	<!-- Favicons -->
	<link rel="apple-touch-icon" href="<?php echo get_template_directory_uri(); ?>/images/apple-touch-icon.png">
	<link rel="apple-touch-icon" sizes="72x72" href="<?php echo get_template_directory_uri(); ?>/images/apple-touch-icon-72x72.png">
	<link rel="apple-touch-icon" sizes="114x114" href="<?php echo get_template_directory_uri(); ?>/images/apple-touch-icon-114x114.png">
	
	<!-- Custom CSS -->
	<?php if(get_option('t2t_custom_css') != "") { ?>
		<style type="text/css" media="screen">
			<?php echo get_option('t2t_custom_css'); ?>
		</style>
	<?php } ?>
	
	<!-- Custom JS -->
	<?php if(get_option('t2t_custom_js') != "") { ?>
		<script type="text/javascript" charset="utf-8">
			<?php echo stripslashes(get_option('t2t_custom_js')); ?>
		</script>
	<?php } ?>
	
	<!-- Theme Hook -->
	<?php wp_head(); ?>
	
</head>
<body <?php body_class(); ?>>
	<!-- Start Wrapper -->
	<div id="page_wrapper">
		
	<!-- Start Header -->
	<header>
		<div class="container">
			<!-- Start Social Icons -->
			<aside>
				<?php t2t_social_links(); ?>
			</aside>
			<!-- End Social Icons -->
			
			<!-- Start Navigation -->
			<nav>
				<?php t2t_menu(); ?>
				<span class="arrow"></span>
			</nav>
			<!-- End Navigation -->
		</div>
	</header>
	<!-- End Header -->
	
	<section class="container">
		
		<!-- Start App Info -->
		<div id="app_info">
			<!-- Start Logo -->
			<?php t2t_logo(); ?>
			<!-- End Logo -->
			<?php if(get_option('t2t_logo_tagline') == true) { ?>
				<span class="tagline"><?php echo get_bloginfo( 'description' ) ?></span>
			<?php } ?>
			<p>
				<?php echo stripslashes(get_option('t2t_app_description')); ?>
			</p>

			<div class="buttons">
				<?php t2t_app_store_buttons(); ?>
			</div>

			<?php if(get_option('t2t_promo_text')) { ?>
			<div class="price <?php echo get_option('t2t_promo_text_arrow'); ?>">
				<p><?php echo stripslashes(get_option('t2t_promo_text')); ?></p>
			</div>
			<?php } ?>

		</div>
		<!-- End App Info -->
		
		<!-- Start Pages -->
		<div id="pages">
			<div class="top_shadow"></div>