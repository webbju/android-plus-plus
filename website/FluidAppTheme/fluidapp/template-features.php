<?php
/*
Template Name: Features
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
		$query->query('post_type=t2t_features&nopaging=true');  
	?>
	
	<div class="feature_list content_box">
		<?php 
			$start = 1;
			while ($query->have_posts()) : $query->the_post();  
		?>  
		<div class="one_half<?php if(is_multiple($start, 2)) { echo " column_last"; } ?>">
			<div class="feature_heading">				
				<h3 class="<?php t2t_feature_class($post->ID); ?>">
					<?php 
						if(get_the_post_thumbnail($post->ID, 'feature-icon')) {
							echo get_the_post_thumbnail($post->ID, 'feature-icon');
						} else if(get_post_meta($post->ID, '_icon', true)) {
							
					?>
						<span class="icon" style="color: <?php echo get_post_meta($post->ID, '_icon_color', true); ?>;"><?php echo get_post_meta($post->ID, '_icon', true); ?></span> 
					<?php } ?>
					<?php echo get_post_meta('_read_more', $post->ID, true);?>
					<?php
						$title_link = get_post_meta($post->ID, '_read_more', true);
						if($title_link == 1) { 
					?>
						<a href="<?php the_permalink(); ?>"><?php the_title(); ?></a>
					<?php } else { ?>
						<?php the_title(); ?>
					<?php } ?>
				</h3>
			</div>
			<?php the_excerpt(); ?>
		</div>
		<?php 
			$start++;
			endwhile; 
			wp_reset_query(); 
		?>	
	</div>
	
</div>
<?php get_footer(); ?>