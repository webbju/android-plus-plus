<?php
/*
Template Name: Updates
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
		$query->query('post_type=t2t_updates&nopaging=true');  
	?>

	<div class="releases">
		<?php 
			$start = 1;
			while ($query->have_posts()) : $query->the_post();  
		?>
		<article class="release">
			<h3>
				<?php
					$title_link = get_post_meta($post->ID, '_read_more', true);
					if($title_link == 1) { 
				?>
					<a href="<?php the_permalink(); ?>"><?php the_title(); ?></a>
				<?php } else { ?>
					<?php the_title(); ?>
				<?php } ?>
			</h3>
			<span class="date"><?php echo get_post_meta($post->ID, '_release_date', true); ?></span>
			<div class="clear"></div>
			<?php the_content(); ?>
		</article>
		<?php 
			$start++;
			endwhile; 
			wp_reset_query(); 
		?>
	</div>
	
</div>
<?php get_footer(); ?>