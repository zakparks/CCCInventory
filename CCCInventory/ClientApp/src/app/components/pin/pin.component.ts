import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { StaffService } from '../../services/staff.service';
import { PinSessionService } from '../../services/pin-session.service';
import { InactivityService } from '../../services/inactivity.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-pin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pin.component.html'
})
export class PinComponent implements OnInit, OnDestroy {
  enteredPin = '';
  errorMessage = '';
  validating = false;

  constructor(
    private staffService: StaffService,
    private pinSession: PinSessionService,
    private inactivity: InactivityService,
    private auth: AuthService,
    private router: Router
  ) { }

  ngOnInit() {
    this.inactivity.stop();
  }

  ngOnDestroy() { }

  appendDigit(digit: string) {
    if (this.enteredPin.length < 4) {
      this.enteredPin += digit;
      this.errorMessage = '';
      if (this.enteredPin.length === 4) {
        this.validatePin();
      }
    }
  }

  backspace() {
    this.enteredPin = this.enteredPin.slice(0, -1);
    this.errorMessage = '';
  }

  onPinInput() {
    this.enteredPin = this.enteredPin.replace(/\D/g, '').slice(0, 4);
    this.errorMessage = '';
    if (this.enteredPin.length === 4) {
      this.validatePin();
    }
  }

  validatePin() {
    if (this.validating) return;
    this.validating = true;

    this.staffService.validatePin(this.enteredPin).subscribe({
      next: (member) => {
        this.validating = false;
        this.pinSession.setStaffMember(member.id, member.name);
        this.inactivity.start();
        this.router.navigate(['/']);
      },
      error: () => {
        this.validating = false;
        this.errorMessage = 'Incorrect PIN. Please try again.';
        this.enteredPin = '';
      }
    });
  }

  logout() {
    this.auth.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
