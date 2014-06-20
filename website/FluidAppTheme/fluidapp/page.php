<?php get_header(); ?>
<div class="page">
	<h1><?php the_title(); ?></h1>	
	
	<?php if (have_posts()) : while (have_posts()) : the_post(); ?>
		<?php the_content(); ?>
	<?php endwhile; endif; ?>

	<?php wp_link_pages('before=<div class="pagination">&after=</div>'); ?>
	
</div>
<?php get_footer(); ?>