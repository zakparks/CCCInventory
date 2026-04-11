import { Component, OnInit } from '@angular/core';
import { NgClass } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { PinSessionService } from '../../services/pin-session.service';
import { InactivityService } from '../../services/inactivity.service';
import { StaffService } from '../../services/staff.service';

@Component({
  selector: 'app-nav-menu',
  standalone: true,
  imports: [NgClass, RouterLink, RouterLinkActive],
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent implements OnInit {
  isExpanded = false;
  activeStaffName = '';

  constructor(
    private pinSession: PinSessionService,
    private inactivity: InactivityService,
    private staffService: StaffService,
    private router: Router
  ) { }

  ngOnInit() {
    const stored = this.pinSession.activeStaffMemberName;
    if (stored) {
      this.activeStaffName = stored;
    } else if (this.pinSession.isActive) {
      // Session was created before name storage was added — look up once and backfill
      this.staffService.getAll().subscribe(members => {
        const match = members.find(m => m.id === this.pinSession.activeStaffMemberId);
        if (match) {
          this.activeStaffName = match.name;
          this.pinSession.setStaffMember(match.id, match.name);
        }
      });
    }
  }

  get pinActive(): boolean {
    return this.pinSession.isActive;
  }

  collapse() {
    this.isExpanded = false;
  }

  toggle() {
    this.isExpanded = !this.isExpanded;
  }

  switchUser() {
    this.inactivity.stop();
    this.pinSession.clear();
    this.router.navigate(['/pin']);
  }
}
