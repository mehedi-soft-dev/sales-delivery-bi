import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Compass } from '@primeicons/angular/compass';

@Component({
  selector: 'app-not-found-page',
  imports: [Compass, RouterLink],
  templateUrl: './not-found.page.html',
  styleUrl: './not-found.page.css',
})
export class NotFoundPage {}
