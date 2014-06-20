<?php get_header(); ?>
<div class="page">
		
<?php if (have_posts()) : while (have_posts()) : the_post(); ?>

	<div class="releases">
		<article class="release">
			<h3><?php the_title(); ?></h3>
			<span class="date"><?php echo get_post_meta($post->ID, '_release_date', true); ?></span>
			<div class="clear"></div>
			<?php the_content(); ?>
		</article>
	</div>
	
	<!-- Start Comments -->
  <div id="comments">
    <?php comments_template('', true); ?>
  </div>
  <!-- End Comments -->
	
<?php endwhile; endif; ?>
	
</div>
<?php get_footer(); ?>