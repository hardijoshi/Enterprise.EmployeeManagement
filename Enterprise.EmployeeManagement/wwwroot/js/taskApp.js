var app = angular.module('taskApp', []);

app.filter('customDate', function () {
    return function (dateString) {
        if (!dateString) return '';
        var date = new Date(dateString);
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], {
            hour: '2-digit',
            minute: '2-digit'
        });
    };
});  


app.controller('taskController', function ($scope, $http) {

    $http.get('/api/user/current')  // This endpoint should return the current user's data (role, name, etc.)
        .then(function (response) {
            // Successfully fetched user data, update the scope
            $scope.currentUser = response.data;
            console.log('Current User:', $scope.currentUser);  // For debugging purposes
        })
        .catch(function (error) {
            console.error('Error fetching current user:', error);
        });

    $scope.successMessage = null; // Variable to store success message

    // This function will be used to clear the success message
    $scope.clearSuccessMessage = function () {
        $scope.successMessage = null;
    };

    console.log('Current User:', $scope.currentUser);

    $scope.formTitle = "Add New Task";
    $scope.buttonText = "Save Task";
    $scope.tasks = [];
    $scope.employees = [];
    $scope.assignedEmployees = [];
    $scope.reviewers = [];
    $scope.selectedTask = null;
    $scope.newStatus = null;
    $scope.statusUpdateError = null;

    $scope.cleanupModal = function (modalId) {
        $(modalId).modal('hide');
        $('body').removeClass('modal-open');
        $('.modal-backdrop').remove();
        $('body').css('padding-right', '');
    };

    $scope.prepareStatusUpdate = function (task) {
        $scope.selectedTask = task;
        $scope.newStatus = task.status;
        $scope.statusUpdateError = null;
    };

    $scope.resetForm = function () {
        $scope.task = {
            taskId: 0,
            title: '',
            description: '',
            isCompleted: false,
            assignedEmployeeId: '',
            reviewerId: ''
        };
        $scope.formTitle = "Add New Task";
        $scope.buttonText = "Add Task";
    };

    $scope.loadData = function () {
        $http.get('/api/employees')
            .then(function (response) {
                $scope.employees = response.data;
                $scope.assignedEmployees = $scope.employees.filter(emp => emp.role === 'Employee');
                $scope.reviewers = $scope.employees.filter(emp => emp.role === 'Manager');
            })
            .catch(function (error) {
                console.error('Error loading employees:', error);
            });

        $scope.loadTasks();
    };

    $scope.loadTasks = function () {
        $http.get('/api/Tasks')
            .then(function (response) {
                $scope.tasks = response.data;
            })
            .catch(function (error) {
                console.error('Error loading tasks:', error);
            });
    };

    $scope.validateTask = function () {
        if (!$scope.task.title?.trim()) {
            alert('Title is required');
            return false;
        }
        if (!$scope.task.assignedEmployeeId) {
            alert('Please select an assigned employee');
            return false;
        }
        if (!$scope.task.reviewerId) {
            alert('Please select a reviewer');
            return false;
        }

        // Date validation
        const startDate = new Date($scope.task.startDate);
        const deadlineDate = new Date($scope.task.deadlineDate);
        const now = new Date();

        if (isNaN(startDate.getTime())) {
            alert('Please enter a valid start date');
            return false;
        }
        if (isNaN(deadlineDate.getTime())) {
            alert('Please enter a valid deadline date');
            return false;
        }
        if (deadlineDate < startDate) {
            alert('Deadline date cannot be earlier than start date');
            return false;
        }
        if ($scope.task.taskId === 0 && startDate < now) {
            alert('Start date cannot be in the past for new tasks');
            return false;
        }

        return true;
    };

    $scope.saveTask = function () {
        if (!$scope.validateTask()) {
            return;
        }

        var taskData = {
            taskId: $scope.task.taskId,
            title: $scope.task.title.trim(),
            description: $scope.task.description?.trim() || '',
            status: $scope.task.taskId === 0 ? 0 : $scope.task.status, // Default to NotStarted for new tasks
            assignedEmployeeId: parseInt($scope.task.assignedEmployeeId),
            reviewerId: parseInt($scope.task.reviewerId),
            startDate: new Date($scope.task.startDate).toISOString(),
            deadlineDate: new Date($scope.task.deadlineDate).toISOString()
        };

        console.log('Sending task data:', taskData);

        // Choose POST or PUT based on taskId
        const isNewTask = taskData.taskId === 0;
        const method = isNewTask ? 'POST' : 'PUT';
        const url = isNewTask ? '/api/Tasks' : `/api/Tasks/${taskData.taskId}`;

        $http({
            method: method,
            url: url,
            data: taskData
        })
            .then(function (response) {
                console.log('Save response:', response);
                $scope.loadTasks();
                $scope.successMessage = 'Task created successfully!';

                // Close the modal after a short delay
                setTimeout(function () {
                    $scope.cleanupModal('#taskModal');
                }, 1500);

                $scope.resetForm();
                //alert('Task saved successfully!');
            })
            .catch(function (error) {
                console.error('Error saving task:', error);
                let errorMessage = 'Error saving task: ';
                if (error.data && error.data.errors) {
                    errorMessage += Object.values(error.data.errors).join(', ');
                } else if (error.data && error.data.title) {
                    errorMessage += error.data.title;
                } else if (error.data) {
                    errorMessage += error.data;
                } else {
                    errorMessage += 'Please try again.';
                }
                alert(errorMessage);
            });
    };

    $scope.editTask = function (task) {
        $scope.task = {
            taskId: task.taskId,
            title: task.title,
            description: task.description || '',
            status: task.status,
            assignedEmployeeId: task.assignedEmployeeId.toString(),
            reviewerId: task.reviewerId.toString(),
            startDate: new Date(task.startDate),
            deadlineDate: new Date(task.deadlineDate)
        };
        $scope.formTitle = "Edit Task";
        $scope.buttonText = "Update Task";
    };

    $scope.deleteTask = function (taskId) {
        if (confirm('Are you sure you want to delete this task?')) {
            $http.delete('/api/Tasks/' + taskId)
                .then(function () {
                    $scope.loadTasks();
                    alert('Task deleted successfully!');
                })
                .catch(function (error) {
                    console.error('Error deleting task:', error);
                    alert('Error deleting task. Please try again.');
                });
        }
    };
    //$scope.updateTaskStatus = function (task) {
    //    // Example logic: Toggle the task's completion status
    //    task.isCompleted = !task.isCompleted;

    //    // Simulate a backend call (replace this with actual API call)
    //    console.log(`Task ${task.title} status updated to: ${task.isCompleted ? 'Completed' : 'Pending'}`);
    //};

    $scope.getStatusText = function (status) {
        switch (parseInt(status)) {
            case 0:
                return 'Not Started';
            case 1:
                return 'Working';
            case 2:
                return 'Pending';
            case 3:
                return 'Completed';
            default:
                return 'Unknown';
        }
    };

    $scope.isValidTransition = function (newStatus) {
        if (!$scope.selectedTask) return false;

        const currentStatus = parseInt($scope.selectedTask.status);
        newStatus = parseInt(newStatus);

        switch (currentStatus) {
            case 0: // NotStarted
                return newStatus === 1; // Can only move to Working
            case 1: // Working
                return newStatus === 2 || newStatus === 3; // Can move to Pending or Completed
            case 2: // Pending
                return newStatus === 1 || newStatus === 3; // Can move to Working or Completed
            case 3: // Completed
                return false; // Cannot move from Completed
            default:
                return false;
        }
    };

    $scope.updateTaskStatus = function () {
        if (!$scope.selectedTask || !$scope.newStatus) return;

        console.log('Updating status for task:', $scope.selectedTask);
        console.log('New status:', $scope.newStatus);

        $http.patch(`/api/tasks/${$scope.selectedTask.taskId}/status`, $scope.newStatus)
            .then(function (response) {
                console.log('Status update response:', response);

                // Force status change date update
                $scope.selectedTask.statusChangeDate = new Date().toISOString();
                $scope.selectedTask.status = $scope.newStatus;

                $scope.loadTasks();
                $scope.successMessage = 'Status Updated successfully!';

                setTimeout(function () {
                    $scope.cleanupModal('#statusModal');
                }, 1500);
            })
            .catch(function (error) {
                console.error('Status update error:', error);
                $scope.statusUpdateError = error.data || 'Error updating task status';
            });
    };

    //$scope.getStatusText = function (status) {
    //    switch (parseInt(status)) {
    //        case 0: return 'Not Started';
    //        case 1: return 'Working';
    //        case 2: return 'Pending';
    //        case 3: return 'Completed';
    //        default: return 'Unknown';
    //    }
    //};

    $scope.getDaysInStatus = function (task) {
        if (!task?.statusChangeDate) return 0;

        try {
            const changeDate = new Date(task.statusChangeDate);
            if (isNaN(changeDate.getTime())) return 0;

            const now = new Date();
            const diffTime = Math.abs(now - changeDate);
            return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
        } catch (e) {
            console.error('Error calculating days in status:', e);
            return 0;
        }
    };
    $scope.getTaskProgress = function (task) {
        if (!task.startDate || !task.deadlineDate) return 0;

        var start = new Date(task.startDate);
        var end = new Date(task.deadlineDate);
        var current = new Date();

        // If task is completed, return 100%
        if (task.status === 3) return 100;

        // Calculate progress based on time elapsed
        var totalDuration = end - start;
        var elapsed = current - start;
        var progress = (elapsed / totalDuration) * 100;

        // Constrain progress between 0 and 100
        return Math.min(Math.max(Math.round(progress), 0), 100);
    };

    $scope.getTimeElapsed = function (task) {
        var start = new Date(task.startDate);
        var end = new Date(task.deadlineDate);
        var now = new Date();

        if (task.status === 3) { // If completed
            return 100;
        }

        // Calculate percentage of time elapsed
        var totalDuration = end - start;
        var elapsed = now - start;
        var percentage = Math.min(Math.round((elapsed / totalDuration) * 100), 100);

        return Math.max(0, percentage); // Ensure we don't return negative values
    };

    $scope.getTimeRemaining = function (task) {
        if (!task.deadlineDate) return 'No deadline';

        var deadline = new Date(task.deadlineDate);
        var current = new Date();
        var diff = deadline - current;

        if (diff < 0) return 'Overdue';

        var days = Math.floor(diff / (1000 * 60 * 60 * 24));
        var hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));

        if (days > 0) {
            return days + ' day' + (days === 1 ? '' : 's') + ' left';
        } else if (hours > 0) {
            return hours + ' hour' + (hours === 1 ? '' : 's') + ' left';
        } else {
            return 'Due soon';
        }
    };

    $scope.sendReminder = function (task) {
        $http.post(`/api/Tasks/${task.taskId}/send-reminder`)
            .then(function (response) {
                alert('Reminder sent successfully!');
            })
            .catch(function (error) {
                console.error('Error sending reminder:', error);
                alert('Error sending reminder. Please try again.');
            });
    };


    // Helper function to format the date string
    $scope.formatDate = function (dateString) {
        if (!dateString) return '';
        var date = new Date(dateString);
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], {
            hour: '2-digit',
            minute: '2-digit'
        });
    };


    // Initialize the page
    $scope.resetForm();
    $scope.loadData();
});

