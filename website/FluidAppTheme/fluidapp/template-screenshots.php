<?php
/*
Template Name: Screenshots
*/
?>
<?php get_header(); ?>
<div class="page">
		
	<?php while (have_posts()) : the_post(); ?>

		<h1>
			<?php 
				global $post;
				the_title();
			?>
		</h1>

		<?php the_content(); ?>
	
	<?php endwhile; ?>
	
	<?php 
		$query = new WP_Query();
		$query->query('post_type=t2t_screenshots&nopaging=true');  
	?>
	
	<div class="screenshot_grid content_box">
		<?php 
			$start = 1;
			while ($query->have_posts()) : $query->the_post();  
		?>  
		<div class="one_third<?php if(is_multiple($start, 3)) { echo " column_last"; } ?>">
			<?php
				$thumb = get_the_post_thumbnail($post->ID, 'screenshot-thumb');
				$original = wp_get_attachment_image_src(get_post_thumbnail_id($post->ID), 'original');
			
			?>
			<?php t2t_lightbox(get_the_ID(), 'screenshot-thumb'); ?>
		</div>
		<?php 
			$start++;
			endwhile; 
			wp_reset_query(); 
		?>	
	</div>
	
</div>
<?php get_footer(); ?>