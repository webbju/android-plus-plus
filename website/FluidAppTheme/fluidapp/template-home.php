<?php
/*
Template Name: Home
*/
?>
<?php get_header(); ?>
	
<!-- Start Home -->
<div id="home" class="page">
	
	<div id="slider">
		
		<?php if(get_option('t2t_disable_slider') == "true") { ?>
			<?php t2t_slide('t2t_slide_1'); ?>
		<?php } else { ?>
			<?php t2t_slide('t2t_slide_1'); ?>
			<?php t2t_slide('t2t_slide_2'); ?>
			<?php t2t_slide('t2t_slide_3'); ?>
			<?php t2t_slide('t2t_slide_4'); ?>
			<?php t2t_slide('t2t_slide_5'); ?>
		<?php } ?>
		
	</div>
	<?php if(get_option('t2t_disable_slider') != "true") { ?>
	<script type="text/javascript" charset="utf-8">
		jQuery(document).ready(function($) {
			// Home slider
			$("#slider").echoSlider({
				effect: "<?php echo get_option('t2t_slider_animation'); ?>", // Default effect to use, supports: "slide" or "fade"
				<?php if(get_option('t2t_disable_easing') == "true") { ?>
				easing: false, // Easing effect for animations
				<?php } ?>
				pauseTime: <?php echo get_option('t2t_pause_duration'); ?>, // How long each slide will appear
				animSpeed: <?php echo get_option('t2t_autoplay_duration'); ?>, // Speed of slide animation 
				<?php if(get_option('t2t_autoplay_duration') == "true") { ?>
				manualAdvance: true, // Disable manual transitions
				<?php } ?>
				pauseOnHover: true, // Pause on mouse hover
				<?php if(get_option('t2t_disable_slider') == "true") { ?>
				controlNav: false,
				<?php } else { ?>
				controlNav: true, // Show slider navigation
				<?php } ?>
				swipeNav: true // Enable touch gestures to control slider
			});
		});
	</script>
	<?php } ?>

</div>
<!-- End Home -->

<?php get_footer(); ?>