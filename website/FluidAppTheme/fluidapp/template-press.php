<?php
/*
Template Name: Press
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
		$query->query('post_type=t2t_press&nopaging=true');  
	?>
	
	<div class="press_mentions">
		<ul>
			<?php while ($query->have_posts()) : $query->the_post(); ?>
			<li>
				<div class="logo">
					<?php echo get_the_post_thumbnail($post->ID, 'press-icon') ?>
				</div>
				<div class="details">
					<?php the_content(); ?>
					<address>
						<?php echo get_post_meta($post->ID, '_author', true); ?>
						<a href="<?php echo get_post_meta($post->ID, '_website', true); ?>"><?php echo get_post_meta($post->ID, '_website', true); ?> &#x2192;</a>
					</address>
				</div>
			</li>
			<?php endwhile; wp_reset_query(); ?>
		</ul>
	</div>
	
</div>
<?php get_footer(); ?>