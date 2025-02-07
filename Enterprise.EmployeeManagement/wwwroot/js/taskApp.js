var app = angular.module('taskApp', []);

app.filter('customDate', function () {
    return function (dateString) {
        if (!dateString) return '';

        const date = new Date(dateString);

        // Format the date parts
        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const year = date.getFullYear();
        let hours = date.getHours();
        const minutes = String(date.getMinutes()).padStart(2, '0');
        const ampm = hours >= 12 ? 'PM' : 'AM';
        hours = hours % 12;
        hours = hours ? hours : 12;

        return `${day}/${month}/${year} ${String(hours).padStart(2, '0')}:${minutes} ${ampm}`;
    };
});

app.controller('taskController', function ($scope, $http, $timeout) {

    $http.get('/api/user/current')
        .then(function (response) {

            $scope.currentUser = response.data;
            console.log('Current User:', $scope.currentUser);
        })
        .catch(function (error) {
            console.error('Error fetching current user:', error);
        });

    $scope.messages = {
        success: null,
        error: null
    };

    $scope.clearMessages = function () {
        $scope.messages.success = null;
        $scope.messages.error = null;
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

        setTimeout(function () {
            $('.modal-backdrop').remove();
            $('body').removeClass('modal-open');
        }, 500); 
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
            status: 0,
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

        const startDate = new Date($scope.task.startDate);
        const deadlineDate = new Date($scope.task.deadlineDate);

        const formatLocalDateTime = (date) => {
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            const hours = String(date.getHours()).padStart(2, '0');
            const minutes = String(date.getMinutes()).padStart(2, '0');
            const seconds = '00';
            return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
        };

        var taskData = {
            taskId: $scope.task.taskId,
            title: $scope.task.title.trim(),
            description: $scope.task.description?.trim() || '',
            status: $scope.task.taskId === 0 ? 0 : $scope.task.status,
            assignedEmployeeId: parseInt($scope.task.assignedEmployeeId),
            reviewerId: parseInt($scope.task.reviewerId),
            startDate: formatLocalDateTime(startDate),
            deadlineDate: formatLocalDateTime(deadlineDate)
        };

        const isNewTask = taskData.taskId === 0;
        const method = isNewTask ? 'POST' : 'PUT';
        const url = isNewTask ? '/api/Tasks' : `/api/Tasks/${taskData.taskId}`;

        $http({
            method: method,
            url: url,
            data: taskData,
            headers: {
                'X-TimeZone-Offset': new Date().getTimezoneOffset()
            }
        }).then(function (response) {
            console.log('Save response:', response);
            $scope.loadTasks();


            $scope.messages.success = response.data.message || (isNewTask ? 'Task created successfully!' : 'Task updated successfully!');
            console.log('Success Message:', $scope.messages.success);



            $('#taskModal').modal('hide').on('hidden.bs.modal', function () {
                $('.modal-backdrop').remove(); // Ensures the backdrop is removed
            });

            // Keep the success message visible for 3 seconds after the modal closes
            $timeout(function () {
                $scope.messages.success = null;
            }, 3000);

            $scope.resetForm();
        }).catch(function (error) {
            console.error('Error saving task:', error);
            $scope.messages.error = error.data.message || 'An error occurred while saving the task.';
        });
    };

    $scope.formatDateForInput = function (dateString) {
        if (!dateString) return '';

        // Create a new date object and handle timezone offset
        const date = new Date(dateString);
        if (isNaN(date.getTime())) return '';

        return date.toISOString().slice(0, 16);
    };

    $scope.editTask = function (task) {

        $scope.messages.success = null;
        const startDate = new Date(task.startDate);
        const deadlineDate = new Date(task.deadlineDate);

        $scope.task = {
            taskId: task.taskId,
            title: task.title,
            description: task.description || '',
            status: task.status,
            assignedEmployeeId: task.assignedEmployeeId.toString(),
            reviewerId: task.reviewerId.toString(),
            startDate: $scope.formatDateForInput(startDate),
            deadlineDate: $scope.formatDateForInput(deadlineDate)
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
        if ($scope.selectedTask && $scope.newStatus === 3) {
            if (!$scope.selectedTask.completedDate) {
                $scope.selectedTask.completedDate = new Date().toISOString(); // Store full timestamp
            }
        } else {
            $scope.selectedTask.completedDate = null; // Reset if status changes back
        }

        $http.patch(`/api/tasks/${$scope.selectedTask.taskId}/status`, $scope.newStatus)
            .then(function (response) {
                console.log('Status update response:', response);

                // Force status change date update
                $scope.selectedTask.statusChangeDate = new Date().toISOString();
                $scope.selectedTask.status = $scope.newStatus;

                $scope.loadTasks();
                $scope.messages.success = response.data.message || 'Reminder sent successfully!';

                setTimeout(function () {
                    $scope.cleanupModal('#statusModal');
                }, 1500);
            })
            .catch(function (error) {
                console.error('Error sending reminder:', error);
                $scope.messages.error = error.data.message || 'Error sending reminder';
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

        return Math.max(0, percentage);
    };

    $scope.getTimeRemaining = function (task) {
        const now = new Date();
        const dueDate = new Date(task.deadlineDate);
        const completionDate = task.completedDate ? new Date(task.completedDate) : null;

        if (task.status === 3) {
            return completionDate && completionDate > dueDate ? "Completed Late" : "Completed On Time";
        }

        if (now > dueDate) return "Overdue";

        const timeDiff = dueDate - now;
        const hoursRemaining = Math.floor(timeDiff / (1000 * 60 * 60));
        const minutesRemaining = Math.floor((timeDiff % (1000 * 60 * 60)) / (1000 * 60));

        return `${hoursRemaining}h ${minutesRemaining}m left`;
    };





    $scope.sendReminder = function (task) {
        $http.post(`/api/Tasks/${task.taskId}/send-reminder`)
            .then(function (response) {
                alert('Reminder sent successfully!');
            })
            .catch(function (error) {
                console.error('Error sending reminder:', error);
                const message = error.data?.data || 'Error sending reminder. Please try again.';
                alert(message);
            });
    };



    // Helper function to format the date string
    $scope.formatDate = function (dateString) {
        if (!dateString) return '';
        var date = new Date(dateString);

        // Convert 24-hour to 12-hour format
        let hours = date.getHours();
        const minutes = String(date.getMinutes()).padStart(2, '0');
        const ampm = hours >= 12 ? 'PM' : 'AM';
        hours = hours % 12;
        hours = hours ? hours : 12; // Convert 0 to 12
        hours = String(hours).padStart(2, '0');

        return `${date.toLocaleDateString()} ${hours}:${minutes} ${ampm}`;
    };



    // Initialize the page
    $scope.resetForm();
    $scope.loadData();
});
